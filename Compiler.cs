#pragma warning disable IDE0079
#pragma warning disable IDE0305
#pragma warning disable SYSLIB1045
#pragma warning disable IDE0057

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Mail;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace OneInsArch
{
    public class Compiler<TSigned, TAddress>
        where TSigned  : INumber<TSigned>
        where TAddress : INumber<TAddress>
    {
        private readonly Regex labelRegex = new(@"^([A-Za-z0-9_\-$@]+):(.*)$");

        private readonly int SizeOfInstruction = SignedSize * 2 + AddressSize;

        public static int SignedSize => Unsafe.SizeOf<TSigned>();
        public static int AddressSize => Unsafe.SizeOf<TAddress>();

        public static string Address(object value) => $"[{value}]";

        public static byte[] ToBytes(TSigned value)
        {
            byte[] bytes = new byte[SignedSize];
            Unsafe.As<byte, TSigned>(ref bytes[0]) = value;
            return bytes;
        }

        public byte[] Compile(
            string text,
            bool emitDebugInfo,
            string? entryPoint)
        {
            IO.Log($"DEFAULT WORD SIZE IS {SignedSize * 8} BITS ({typeof(TSigned).Name})", null);

            text = $@"
                {(entryPoint == null ? string.Empty : $"jmp {entryPoint}")}
                {text}
                $:
                @:
                $t: dws 0
                $e: dws 0
            ";

            List<sbyte> image = [];
            int bytesPlaced = 0;
            List<string> lines = text.Split('\n').ToList();
            List<Label> definedLabels = [];
            int lineIndex = 0;
            bool writeToImage = false;
            int startPlaced;

            long start = Stopwatch.GetTimestamp();

            IO.Log("STARTING PREPROCESSING STAGE...", lineIndex); 
            ProcessCode();
            for (int i = 0; i < VirtualLiteral.DefinedLiterals.Count; i++)
            {
                lineIndex++;
                long value = VirtualLiteral.DefinedLiterals[i].Value;
                lines.Add($"dws {value}");
                int offset = bytesPlaced + (i * SignedSize);
                VirtualLiteral.DefinedLiterals[i].Offset = offset;
                IO.Log($"DEFINE VIRTUAL LITERAL {value} at {offset}", lineIndex);
            }
            bytesPlaced = 0;
            writeToImage = true;
            lineIndex = 0;
            IO.Log("STARTING PROCESSING STAGE...", lineIndex);
            ProcessCode();

            long end = Stopwatch.GetTimestamp();
            double elapsed = (end - start) / (double)Stopwatch.Frequency;

            IO.Log($"Program image size is {image.Count} bytes", null);

            List<byte> unsignedImage = image.ConvertAll(b => unchecked((byte)b));

            if (emitDebugInfo)
                unsignedImage.AddRange(DebugInfo.EncodeDebugInfo(definedLabels));

            Signature.AppendSignature(
                ref unsignedImage,
                (byte)(SignedSize * 8),
                (byte)(AddressSize * 8));

            IO.Log($"VIRTUAL LITERALS: {VirtualLiteral.AsJson()}", null);

            IO.Log($"Total image size is {unsignedImage.Count} bytes", null);

            IO.Print(
                $"Compilation finished after {IO.FormatElapsed(elapsed)}",
                ConsoleColor.Green);

            return unsignedImage.ToArray();

            void ProcessCode()
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    startPlaced = image.Count;
                    lineIndex++;
                    string line = lines[i].Trim();
                    Match labelMatch = labelRegex.Match(line);
                    if (labelMatch.Success)
                    {
                        string name = labelMatch.Groups[1].Value;
                        string value = labelMatch.Groups[2].Value.TrimStart();
                        bool isLiteral = value.StartsWith("EQU", StringComparison.OrdinalIgnoreCase);
                        bool isRegister = value.StartsWith("REGISTER", StringComparison.OrdinalIgnoreCase);
                        Label newLabel = new(
                            name,
                            bytesPlaced,
                            isLiteral,
                            isLiteral ? value.Substring(3) : null);
                        if (!writeToImage) definedLabels.Add(newLabel);
                        IO.Log($"DEFINE LABEL {newLabel}", lineIndex);
                        IO.Log($"UPDATE LINE \"{line}\" to \"{value}\"", lineIndex);
                        line = value;
                    }
                    switch (line.TrimStart())
                    {
                        // Empty line
                        case var s when string.IsNullOrWhiteSpace(s):
                            IO.Log("SKIPPING EMPTY LINE", lineIndex);
                            break;

                        // Compatibility
                        case var s when Helper.StartsWith(s, "section"):
                            break;

                        // Data definitions
                        case var s when Helper.StartsWith(s, "db"):
                            Define<sbyte>(s);
                            break;
                        case var s when Helper.StartsWith(s, "dw"):
                            Define<short>(s);
                            break;
                        case var s when Helper.StartsWith(s, "dd"):
                            Define<int>(s);
                            break;
                        case var s when Helper.StartsWith(s, "dq"):
                            Define<long>(s);
                            break;
                        case var s when Helper.StartsWith(s, "do"): // octa-word
                            Define<Int128>(s);
                            break;
                        case var s when Helper.StartsWith(s, "dws"): // word-sized
                            Define<TSigned>(s);
                            break;
                        case var s when Helper.StartsWith(s, "das"): // address-sized
                            Define<TAddress>(s);
                            break;
                        case var s when Helper.StartsWith(s, "register"):
                            Define<TSigned>(s);
                            break;
                        case var s when Helper.StartsWith(s, "equ"):
                            IO.Log("SKIPPING EQU", lineIndex);
                            break;

                        // Data reservations
                        case var s when Helper.StartsWith(s, "resb"):
                            Reserve<byte>(s);
                            break;
                        case var s when Helper.StartsWith(s, "resw"):
                            Reserve<short>(s);
                            break;
                        case var s when Helper.StartsWith(s, "resd"):
                            Reserve<int>(s);
                            break;
                        case var s when Helper.StartsWith(s, "resq"):
                            Reserve<long>(s);
                            break;
                        case var s when Helper.StartsWith(s, "reso"): // octa-word
                            Reserve<Int128>(s);
                            break;
                        case var s when Helper.StartsWith(s, "resws"): // word-sized
                            Reserve<TSigned>(s);
                            break;
                        case var s when Helper.StartsWith(s, "resas"): // address-sized
                            Reserve<TAddress>(s);
                            break;

                        // SUBLEQ Instruction
                        case var s when Helper.StartsWith(s, "subleq"):
                            string[] subleqParameters = GetParameters(s, 3);
                            PutSubleq(
                                subleqParameters[0],
                                subleqParameters[1],
                                subleqParameters[2]);
                            break;

                        // Arithmetic
                        case var s when Helper.StartsWith(s, "add"):
                            string[] addParameters = GetParameters(s, 2);
                            bool addDstIsAddress =
                                Numeric.TryAddress(addParameters[1], out string value, lineIndex);
                            IO.Log($"DST IS ADDRESS: {addDstIsAddress}", lineIndex);
                            if (addDstIsAddress)
                                EmitAddR(addParameters[0], addParameters[1]);
                            else
                                EmitAddC(addParameters[0], addParameters[1]);
                            break;
                        case var s when Helper.StartsWith(s, "add_r"):
                            string[] addRParameters = GetParameters(s, 2);
                            EmitAddR(addRParameters[0], addRParameters[1]);
                            break;
                        case var s when Helper.StartsWith(s, "add_c"):
                            string[] addCParameters = GetParameters(s, 2);
                            EmitAddC(addCParameters[0], addCParameters[1]);
                            break;
                        case var s when Helper.StartsWith(s, "sub"):
                            string[] subParameters = GetParameters(s, 2);
                            PutSubleq(subParameters[1], subParameters[0], "@");
                            break;
                        case var s when Helper.StartsWith(s, "inc"):
                            string[] incParameters = GetParameters(s, 1);
                            EmitAddC(incParameters[0], "1");
                            break;
                        case var s when Helper.StartsWith(s, "dec"):
                            string[] decParameters = GetParameters(s, 1);
                            EmitAddC(decParameters[0], "-1");
                            break;

                        // Control Flow
                        case var s when Helper.StartsWith(s, "jmp"):
                            string jmpParameter = GetParameters(s, 1)[0];
                            PutSubleq("$t", "$t", jmpParameter);
                            break;

                        // System
                        case var s when Helper.StartsWith(s, "hlt"):
                            PutSubleq(
                                "$t",
                                "$t",
                                Address(startPlaced));
                            break;
                        case var s when Helper.StartsWith(s, "nop"):
                            PutSubleq("$t", "$t", "@");
                            break;

                        // No match
                        default:
                            IO.Log($"NO MATCH IN \"{line}\"", lineIndex);
                            string keyword = Helper.RemoveAtSpace(line);
                            IO.CodeError(
                                $"Unknown keyword: \"{keyword}\"",
                                lineIndex,
                                line,
                                0,
                                keyword.Length);
                            break;
                    }
                    
                    int compiledBytes = bytesPlaced - startPlaced;
                    if (writeToImage && compiledBytes > 0)
                    {
                        sbyte[] tail = image.GetRange(startPlaced, compiledBytes).ToArray();
                        IO.Log($"Compiled {compiledBytes} bytes ({startPlaced} + {compiledBytes} = {bytesPlaced}) \"{line}\" to {Numeric.FormatSBytes(tail)}", lineIndex);
                    }
                }
            }

            string[] GetParameters(string s, int count)
            {
                int sp = s.IndexOf(' ');
                if (sp < 0)
                    IO.CodeError($"Missing arguments for instruction.", lineIndex, s, 0, s.Length);
                string[] words = s.Substring(sp + 1).Trim().Split(',');
                if (words.Length != count)
                    IO.CodeError($"The instruction got {words.Length} arguments but requires {count}.", lineIndex);
                return words.Select(w => w.Trim()).ToArray();
            }

            void EmitAddC(string dst, string src)
            {
                PutSubleq("n:" + src, dst, "@");
            }

            void EmitAddR(string dst, string src)
            {
                PutSubleq(src, "$t", "@");
                PutSubleq("$t", dst, "@");
                PutSubleq("$t", "$t", "@");
            }

            void PutSubleq(object a, object b, object c)
            {
                DefineWithDefaultWordSizeByArray([a.ToString()!, b.ToString()!]);
                DefineAddress(c.ToString()!);
            }

            void PutBytes(sbyte[] value)
            {
                int len = value.Length;
                long bytesPlacedBefore = bytesPlaced;
                if (writeToImage) image.AddRange(value);
                bytesPlaced += value.Length;
                IO.Log(
                    $"PUT BYTES: {{\"len\":{len},\"value\":\"{Numeric.FormatSBytes(value)}\",\"bytesPlaced\":\"{bytesPlacedBefore}+{len}={bytesPlaced}\"}}",
                    lineIndex);
            }
            
            void DefineWithDefaultWordSizeByArray(string[] words)
            {
                foreach (var word in words)
                {
                    HandleParameter(word, out TSigned value);
                    sbyte[] bytesword = BitTypes.ToSBytes(value);
                    IO.Log($"DEFINE WITH DEFAULT WORD SIZE {typeof(TSigned).Name.ToUpper()} {word.Trim()}={value}={Numeric.FormatSBytes(bytesword)}", lineIndex);
                    PutBytes(bytesword);
                }
            }

            void DefineAddress(string s)
            {
                HandleParameter(s, out TAddress value);
                sbyte[] bytesword = BitTypes.ToSBytes(value);
                IO.Log($"DEFINE WITH ADDRESS SIZE {typeof(TAddress).Name.ToUpper()} {s.Trim()}={value}={Numeric.FormatSBytes(bytesword)}", lineIndex);
                PutBytes(bytesword);
            }

            void Reserve<T>(string s)
                where T : INumber<T>
            {
                string args = Helper.SubstringAtSpace(s) ?? string.Empty;
                string[] words = args.Split(',');
                foreach (var word in words)
                {
                    HandleParameter(word, out int value, true);
                    PutBytes(new sbyte[value * Unsafe.SizeOf<T>()]);
                }
            }

            void Define<T>(string s)
                where T : INumber<T>
            {
                string args = Helper.SubstringAtSpace(s) ?? string.Empty;
                string[] words = args.Split(',');
                foreach (var word in words)
                {
                    HandleParameter(word, out T value, true);
                    sbyte[] bytesword = BitTypes.ToSBytes(value);
                    IO.Log($"DEFINE {typeof(T).Name.ToUpper()} {word.Trim()}={value}={Numeric.FormatSBytes(bytesword)}", lineIndex);
                    PutBytes(bytesword);
                }
            }

            void HandleParameter<T>(
                string value,
                out T result,
                bool isDataDefinition = false)
                where T : INumber<T>
            {
                bool negative = false;
                if (value.StartsWith("n:"))
                {
                    negative = true;
                    value = value.Substring(2);
                }
                bool isAddress = Numeric.TryAddress(value, out string addressResutlt, lineIndex);
                if (isAddress) value = addressResutlt;
                var label = definedLabels.FirstOrDefault(l => l.Name == value.Trim());
                if (label == null)
                {
                    Numeric.ParseNumber(
                        value,
                        lineIndex,
                        out result,
                        writeToImage);
                    if (negative) result = -result;
                    if (!isAddress &&
                        !isDataDefinition &&
                        Numeric.IsNumeric(value))
                        result = 
                            T.CreateChecked
                            (VirtualLiteral.GetOrDefine(
                                long.CreateChecked(result),
                                lineIndex));
                }
                else
                {
                    if (writeToImage && emitDebugInfo)
                        label.UsedAt.Add(bytesPlaced);
                    if (label.Name == "@")
                        result = T.CreateChecked(
                            startPlaced +
                            (bytesPlaced - startPlaced) /
                            SizeOfInstruction *
                            SizeOfInstruction +
                            SizeOfInstruction);
                    else if (label.Name == "$")
                        result = T.CreateChecked(
                            startPlaced +
                            (bytesPlaced - startPlaced) /
                            SizeOfInstruction *
                            SizeOfInstruction);
                    else if (label.IsLiteral)
                    {
                        string? literal = label.Value;
                        if (literal != null)
                            Numeric.ParseNumber(
                                literal,
                                lineIndex,
                                out result,
                                writeToImage);
                        else
                        {
                            IO.Unassigned(label, lineIndex);
                            result = T.Zero;
                        }
                    }
                    else
                        result = T.CreateChecked(label.Offset);
                    if (negative) result = -result;
                }
                IO.Log($"PARAMETER HANDLED \"{value}\" to {result}", lineIndex);
            }
        }
    }
}
