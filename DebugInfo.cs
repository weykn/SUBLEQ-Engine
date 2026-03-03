#pragma warning disable IDE0305

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace OneInsArch
{
    public static class DebugInfo
    {
        private static byte[] ToCString(string? value)
            => Encoding.UTF8.GetBytes((value ?? string.Empty) + '\0');

        public static Label[] DecodeDebugInfoFromImage(byte[] image)
            => DecodeDebugInfo(ExtractDebugInfoFromImage(image));

        private static string ReadCString(byte[] data, ref int pos)
        {
            int start = pos;
            while (data[pos] != 0) pos++;
            string s = Encoding.UTF8.GetString(data, start, pos - start);
            pos++;
            return s;
        }

        private static long ReadInt64(byte[] data, ref int pos)
        {
            long value = BitConverter.ToInt64(data, pos);
            pos += 8;
            return value;
        }

        private static int ReadInt32(byte[] data, ref int pos)
        {
            int value = BitConverter.ToInt32(data, pos);
            pos += 4;
            return value;
        }

        private static byte[] ExtractDebugInfoFromImage(byte[] image)
        {
            int lengthOffset = image.Length - (sizeof(int) + Signature.SignatureLength);
            int debugInfoLength = BitConverter.ToInt32(image, lengthOffset);
            int debugInfoStart = lengthOffset - debugInfoLength;
            byte[] debugInfo = new byte[debugInfoLength];
            Buffer.BlockCopy(image, debugInfoStart, debugInfo, 0, debugInfoLength);
            return debugInfo;
        }

        private static Label[] DecodeDebugInfo(byte[] debugInfo)
        {
            List<Label> definedLables = [];
            int pos = 1;

            while (pos < debugInfo.Length)
            {
                string name = ReadCString(debugInfo, ref pos);
                string value = ReadCString(debugInfo, ref pos);

                long offset = ReadInt64(debugInfo, ref pos);
                bool isLiteral = debugInfo[pos++] != 0;

                int usedAtCount = ReadInt32(debugInfo, ref pos);
                long[]? usedAt = null;

                if (usedAtCount > 0)
                {
                    usedAt = new long[usedAtCount];
                    for (int i = 0; i < usedAtCount; i++)
                        usedAt[i] = ReadInt64(debugInfo, ref pos);
                }

                definedLables.Add(new Label(
                    name,
                    offset,
                    isLiteral,
                    value,
                    usedAt));
            }

            return definedLables.ToArray();
        }

        public static byte[] EncodeDebugInfo(List<Label> definedLabels)
        {
            List<byte> debugInfo = [0x69];
            foreach (Label label in definedLabels)
            {
                IO.Log($"Generating debug info for {label}", null);

                byte[] encodedName = ToCString(label.Name);
                byte[] encodedValue = ToCString(label.Value);
                debugInfo.AddRange(encodedName);
                debugInfo.AddRange(encodedValue);
                debugInfo.AddRange(BitConverter.GetBytes(label.Offset));
                debugInfo.Add((byte)(label.IsLiteral ? 1 : 0));
                if (label.UsedAt == null)
                    debugInfo.AddRange(BitConverter.GetBytes(0));
                else
                {
                    debugInfo.AddRange(BitConverter.GetBytes(label.UsedAt.Count));
                    foreach (long usedAt in label.UsedAt)
                        debugInfo.AddRange(BitConverter.GetBytes(usedAt));
                }
            }

            debugInfo.AddRange(BitConverter.GetBytes(debugInfo.Count));

            IO.Log($"Generated debug info is {debugInfo.Count} bytes", null);

            return debugInfo.ToArray();
        }
    }
}
