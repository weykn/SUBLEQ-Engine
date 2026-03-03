using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace OneInsArch
{
    public class Signature
    {
        public byte WordSize { get; init; }
        public byte FormatVersion { get; init; }
        public byte FirstConstant { get; init; }
        public byte SecondConstant { get; init; }
        public byte BuildVersion { get; init; }
        public byte AddressingMode { get; init; }
        public byte MetadataVersion { get; } = 1;

        public byte[] ToArray()
            => [(byte)(BuildVersion - FormatVersion),
                (byte)(FirstConstant + AddressingMode / 8 - 1),
                (byte)(SecondConstant + WordSize / 8 - 1),
                FormatVersion];

        public override string ToString() => Numeric.FormatBytes(ToArray());

        public string AsJson() =>
            JsonSerializer.Serialize(this);

        public readonly static Signature CurrentSignature = new()
        {
            WordSize = 8,
            FormatVersion = 1,
            BuildVersion = 1,
            FirstConstant = 0x91,
            SecondConstant = 0xA7,
            AddressingMode = 16,
        };

        public const byte SignatureLength = 4;
        public static bool DisableSignature { get; set; } = false;
        public static readonly byte[] LegalWordSizes = BitTypes.LegalBitTypes;

        public static string GetFileName()
        {
            Process currentProcess = Process.GetCurrentProcess();
            string? filePath = currentProcess.MainModule?.FileName;
            string executableName = !string.IsNullOrEmpty(filePath)
                ? Path.GetFileNameWithoutExtension(filePath)
                : currentProcess.ProcessName;
            return executableName;
        }

        public static Signature CreateSignature(byte wordSize, byte addressingMode)
        {
            var sig = new Signature
            {
                FirstConstant = CurrentSignature.FirstConstant,
                SecondConstant = CurrentSignature.SecondConstant,
                FormatVersion = CurrentSignature.FormatVersion,
                WordSize = wordSize,
                BuildVersion = CurrentSignature.BuildVersion,
                AddressingMode = addressingMode,
            };
            return sig;
        }

        public static void WriteFileWithSignature(string path, byte[] image, Signature signature)
        {
            byte[] binarySignature = signature.ToArray();
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            stream.Write(image, 0, image.Length);
            stream.Write(binarySignature, 0, binarySignature.Length);
        }

        public static void AppendSignature(ref List<byte> programImage, byte wordSize, byte addressingMode)
        {
            if (DisableSignature) return;
            Signature sig = CreateSignature(wordSize, addressingMode);
            programImage.AddRange(sig.ToArray());
            IO.Log($"SIGNATURE ADDED: {sig.AsJson()}", null);
        }

        public static (Signature signature, bool success) ReadSignature(byte[] programImage)
        {
            if (programImage.Length < SignatureLength)
            {
                IO.Warning("Program too short to contain a valid signature.");
                return (new Signature(), false);
            }

            Signature sig = DecodeSignature(programImage);

            IO.Log($"SIGNATURE READ {sig} ", null);

            bool valid =
                sig.FirstConstant == CurrentSignature.FirstConstant &&
                sig.SecondConstant == CurrentSignature.SecondConstant &&
                LegalWordSizes.Contains(sig.WordSize) &&
                LegalWordSizes.Contains(sig.AddressingMode);

            if (!valid)
            {
                IO.Warning("Invalid or missing signature. Reverting to default settings...");
                return (sig, false);
            }

            if (sig.FormatVersion > CurrentSignature.FormatVersion)
                IO.Warning($"Binary version ({sig.FormatVersion}) is newer than compiler version ({CurrentSignature.FormatVersion}), compatibility not guaranteed.");
            else if (sig.FormatVersion < CurrentSignature.FormatVersion)
                IO.Warning($"Binary version ({sig.FormatVersion}) is older than compiler version ({CurrentSignature.FormatVersion}), compatibility not guaranteed.");

            return (sig, true);
        }

        public static (byte wordSize, byte addressingMode) CheckSignature(byte[] programImage)
        {
            var (sig, success) = ReadSignature(programImage);
            return success ?
                (sig.WordSize, sig.AddressingMode) :
                (CurrentSignature.WordSize, CurrentSignature.AddressingMode);
        }

        private static Signature DecodeSignature(byte[] programImage)
        {
            int len = programImage.Length;
            var sig = new Signature
            {
                FormatVersion = programImage[--len],
                WordSize = (byte)((programImage[--len] + 1 - CurrentSignature.SecondConstant) * 8),
                SecondConstant = (byte)(programImage[len] - (programImage[len] - CurrentSignature.SecondConstant)),
                AddressingMode = (byte)((programImage[--len] + 1 - CurrentSignature.FirstConstant) * 8),
                FirstConstant = (byte)(programImage[len] - (programImage[len] - CurrentSignature.FirstConstant)),
                BuildVersion = (byte)(programImage[--len] + programImage[^1])
            };
            return sig;
        }
    }
}
