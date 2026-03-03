using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace OneInsArch
{
    public static class Numeric
    {
        public static string FormatSBytes(sbyte[] data) =>
            string.Join('-', data.Select(b => ((byte)b).ToString("X2")));

        public static string FormatBytes(byte[] data) =>
            string.Join('-', data.Select(b => b.ToString("X2")));

        public static bool IsNumeric(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            input = input.Trim();

            if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return long.TryParse(
                    input[2..],
                    NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture,
                    out _
                );
            }

            return long.TryParse(
                input,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out _
            );
        }

        public static bool TryAddress(string input, out string value, int line)
        {
            value = null!;
            input = input.Trim();

            IO.Log($"TRY ADDRESS on \"{input}\"", line);

            if (string.IsNullOrEmpty(input) ||
                input.Length < 3 ||
                input[0] != '[' ||
                input[^1] != ']')
                return false;

            IO.Log($"THE INPUT \"{input}\" is an address.", line);

            value = input.Substring(1, input.Length - 2);
            return true;
        }


        public static void ParseNumber<T>(
            string value,
            int line,
            out T result,
            bool notPreprocessingMode)
            where T : INumber<T>
        {
            value = value.Trim();

            if (string.IsNullOrWhiteSpace(value))
                IO.CodeError("Input cannot be empty.", line);

            if (!notPreprocessingMode)
            {
                bool hex = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                        || value.EndsWith("h", StringComparison.OrdinalIgnoreCase);

                bool dec = long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);

                if (!hex && !dec)
                {
                    result = T.Zero;
                    return;
                }

                string v = value
                    .Replace("0x", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("h", "", StringComparison.OrdinalIgnoreCase);

                if (!T.TryParse(
                        v,
                        hex ? NumberStyles.HexNumber : NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out result!))
                {
                    result = T.Zero;
                    return;
                }

                return;
            }

            result = default!;

            bool isHex = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                || value.EndsWith("h", StringComparison.OrdinalIgnoreCase);

            NumberStyles style = isHex ? NumberStyles.HexNumber : NumberStyles.Integer;

            string cleaned = value
                .Replace("0x", "", StringComparison.OrdinalIgnoreCase)
                .Replace("h", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            try
            {
                long number = long.Parse(cleaned, style, CultureInfo.InvariantCulture);

                int bits = Marshal.SizeOf<T>() * 8;

                if (bits < 64)
                {
                    long mask = (1L << bits) - 1;
                    number &= mask;
                }

                result = (T)BitTypes.CastBitType<T>(number, line);

                IO.Log($"PARSED NUMBER \"{value}\" to {result}", line);
            }
            catch (FormatException)
            {
                IO.CodeError($"Invalid number format: \"{value}\"", line);
            }
            catch (OverflowException)
            {
                IO.CodeError($"Value out of range for type {typeof(T).Name}: \"{value}\"", line);
            }
        }
    }
}
