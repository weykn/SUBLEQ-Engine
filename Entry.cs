#pragma warning disable IDE0079
#pragma warning disable IDE0305
#pragma warning disable SYSLIB1045
#pragma warning disable IDE0057

using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace OneInsArch
{
    internal class Entry
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e)
                => IO.EngineError((Exception)e.ExceptionObject);

            string output = null!;
            string input = null!;
            string? monitorLabel = null;
            bool fileless = false;
            bool runAfter = false;
            bool readSignatureAndExit = false;
            bool printInfoAndExit = false;
            bool ignoreSignature = false;
            bool trySign = false;
            bool emitDebugInfo = false;
            bool readDebugInfoAndExit = false;
            bool showHelpAndExit = false;
            byte bitsInWord = Signature.CurrentSignature.WordSize;
            byte bitsInAddress = 0;
            int memoryCapacity = VirtualMachineInit.DefaultMemoryCapacity;
            long monitorByte = -1;
            int debugExitCode = -1;
            Type wordType;
            Type addressType;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                switch (arg)
                {
                    case "--test-error":
                        if (i + 1 >= args.Length) IO.ArgumentError($"Missing argument after \"{arg}\".");
                        string type = args[++i];
                        IO.Print($"Testing Error Type {type}...");
                        if (type == "arg") IO.ArgumentError("Test Argument Error");
                        if (type == "code") IO.CodeError("Test Code Error", int.MaxValue);
                        if (type == "sys") throw new Exception("Test System Error");
                        IO.Print($"Type {type} is invalid");
                        IO.Exit();
                        break;
                    case "--debug":
                    case "-g":
                        emitDebugInfo = true;
                        break;
                    case "--verbose":
                        IO.Debug = true;
                        break;
                    case "--speed-up":
                        IO.InactivePrint = true;
                        IO.Print("The speed-up feature isn't fully available yet.");
                        break;
                    case "--fileless":
                    case "-f":
                        fileless = true;
                        runAfter = true;
                        break;
                    case "--run":
                    case "-r":
                        runAfter = true;
                        break;
                    case "--debug-exit-code":
                        if (!(i + 1 < args.Length && int.TryParse(args[++i], out debugExitCode)))
                        {
                            IO.ArgumentError($"Invalid or missing argument after \"{arg}\".");
                        }
                        break;
                    case "--debug-monitor":
                        if (i + 1 >= args.Length) IO.ArgumentError($"Missing argument after \"{arg}\".");
                        string monitorArgument  = args[++i];
                        if (!long.TryParse(monitorArgument, out monitorByte))
                            monitorLabel = monitorArgument;
                        break;
                    case "--read-debug":
                        readDebugInfoAndExit = true;
                        break;
                    case "--version":
                    case "-v":
                        IO.Print(
                            $"Build  Version {Signature.CurrentSignature.BuildVersion}\n" +
                            $"Format Version {Signature.CurrentSignature.FormatVersion}");
                        IO.Exit();
                        break;
                    case "--output":
                    case "-o":
                        if (i + 1 < args.Length) output = args[++i];
                        else IO.ArgumentError($"Missing argument after \"{arg}\".");
                        break;
                    case "--rainbow":
                        IO.Rainbow = true;
                        break;
                    case "--word":
                    case "-w":
                        if (i + 1 < args.Length)
                        {
                            string bitsInWordArg = args[++i];
                            if (byte.TryParse(bitsInWordArg, out bitsInWord))
                                IO.Log($"Word size set to {bitsInWord} bits", null);
                            else
                                IO.ArgumentError($"Invalid word size value: \"{bitsInWordArg}\"");
                        }
                        else IO.ArgumentError($"Missing argument after \"{arg}\".");
                        break;
                    case "--no-signature":
                        Signature.DisableSignature = true;
                        break;
                    case "-i":
                    case "--info":
                        printInfoAndExit = true;
                        break;
                    case "--read-signature":
                        readSignatureAndExit = true;
                        break;
                    case "--ignore-signature":
                        ignoreSignature = true;
                        break;
                    case "--try-sign":
                        trySign = true;
                        break;
                    case "--address":
                        if (i + 1 < args.Length)
                        {
                            string bitsInAddressArg = args[++i];
                            if (byte.TryParse(bitsInAddressArg, out bitsInAddress))
                                IO.Log($"Addressing mode set to {bitsInAddress} bits", null);
                            else
                                IO.ArgumentError($"Invalid address size value: \"{bitsInAddressArg}\"");
                        }
                        else IO.ArgumentError($"Missing argument after \"{arg}\".");
                        break;
                    case "--flow-protection":
                        IO.Print("Flow protection is not available yet.");
                        break;
                    case "--memory":
                    case "-m":
                        if (i + 1 >= args.Length) IO.ArgumentError($"Missing argument after \"{arg}\".");
                        if (!int.TryParse(args[++i], out memoryCapacity))
                            IO.ArgumentError($"Invalid argument for memory capacity: \"{args[i]}\"");
                        break;
                    case "--help":
                    case "-h":
                        showHelpAndExit = true;
                        break;
                    default:
                        if (input == null)
                            input = arg;
                        else
                            IO.ArgumentError($"Unknown argument: {arg}");
                        break;
                }
            }

            if (showHelpAndExit)
            {
                IO.ShowHelp();
                IO.Exit();
            }

            if (readSignatureAndExit)
            {
                IO.Print($"Compiler Signature: " + Signature.CurrentSignature.AsJson());

                if (string.IsNullOrEmpty(input))
                    IO.Print("No input file provided.");
                else
                {
                    byte[] programImage = File.ReadAllBytes(input);
                    var (signature, success) = Signature.ReadSignature(programImage);
                    IO.Print("Binary Signature: " + signature.AsJson());
                    if (!success)
                        IO.Print("Signature check failed, invalid or missing signature.");
                    else
                        IO.Print("Signature check succeeded, signature is valid.");
                }

                IO.Exit();
            }

            if (readDebugInfoAndExit)
            {
                if (string.IsNullOrEmpty(input))
                    IO.ArgumentError("No input file provided.");
                else
                {
                    byte[] programImage = File.ReadAllBytes(input);
                    try
                    {
                        Label[] definedLabels = DebugInfo.DecodeDebugInfoFromImage(programImage);

                        foreach (Label label in definedLabels)
                            IO.Print(label.ToString());
                    }
                    catch
                    {
                        IO.ArgumentError("Debug information is missing or invalid.");
                    }
                }
            }

            if (printInfoAndExit)
            {
                if (string.IsNullOrEmpty(input))
                    IO.Print("No input file provided.");
                else
                {
                    byte[] programImage = File.ReadAllBytes(input);
                    var (sig, success) = Signature.ReadSignature(programImage);
                    if (!success)
                        IO.Print("Unable to read file properties: File is unsigned or invalid signature.");
                    else
                        IO.Print($"{Path.GetFileName(input)}: {sig.WordSize}-bit executable, {sig.AddressingMode}-bit addressing, platform version {sig.BuildVersion}:{sig.FormatVersion}:{sig.MetadataVersion}, magic 0x{(ushort)((sig.FirstConstant << 8) | sig.SecondConstant):X4}");
                }

                IO.Exit();
            }

            string DebugFilePath = Path.Combine(
                Environment.CurrentDirectory,
                "use_the_debug_file.sqaa");
            if (File.Exists(DebugFilePath) && input == null)
            {
                input = DebugFilePath;
                IO.Debug = true;
                IO.Log("VERBOSE MODE ENABLED!", null);
            }

            if (string.IsNullOrEmpty(input))
                IO.ArgumentError("No input file defined");

            if (!File.Exists(input))
                IO.ArgumentError($"Unable to find file: {input}");

            if (string.IsNullOrEmpty(output))
                output = Path.Combine(
                    Path.GetDirectoryName(input)!,
                    Path.GetFileNameWithoutExtension(input));

            string text = null!;

            try
            {
                byte[] binary = File.ReadAllBytes(input!);
                bool isCompiled = binary.Any(b => b < 9 || (b > 13 && b < 32) || b > 126);

                if (trySign)
                {
                    IO.ThrowOnCodeError = true;
                    List<Signature> validSignatures = [];
                    int amountOfLegalWordSizes = Signature.LegalWordSizes.Length;
                    int amountOfSignatures = amountOfLegalWordSizes * amountOfLegalWordSizes;
                    for (int i = 0; i < amountOfLegalWordSizes; i++)
                    {
                        byte wordSize = Signature.LegalWordSizes[i];
                        for (int j = 0; j < amountOfLegalWordSizes; j++)
                        {
                            byte addressingMode = Signature.LegalWordSizes[j];
                            Signature sig = Signature.CreateSignature(wordSize, addressingMode);
                            IO.Print(
                                $"({i * amountOfLegalWordSizes + j + 1,2} / {amountOfSignatures}) " +
                                $"Testing signature: {sig}@W{sig.WordSize.ToString().PadLeft(2, '0')}A{addressingMode.ToString().PadLeft(2, '0')} ",
                                ConsoleColor.Gray,
                                newLine: false);
                            bool signatureIsValid = true;
                            try
                            {
                                RunInVirtualMachine(
                                    BitTypes.GetSignedTypeByWidth(wordSize),
                                    BitTypes.GetUnsignedTypeByWidth(addressingMode),
                                    binary,
                                    memoryCapacity,
                                    true,
                                    TimeSpan.FromMilliseconds(50));
                            }
                            catch (Exception ex)
                            {
                                IO.Log($"Reason for failure: {ex}", null);
                                signatureIsValid = false;
                            }
                            if (signatureIsValid)
                            {
                                IO.Print("Success", ConsoleColor.Green);
                                validSignatures.Add(sig);
                            }
                            else IO.Print("Failed", ConsoleColor.Red);
                        }
                    }
                    switch (validSignatures.Count)
                    {
                        case 0:
                            IO.Print("No valid signature found, exiting...", ConsoleColor.DarkRed);
                            break;
                        case 1:
                            Signature.WriteFileWithSignature(input, binary, validSignatures[0]);
                            IO.Print("Matching signature found and added.", ConsoleColor.DarkGreen);
                            break;
                        default:
                            IO.Print("Multiple matching signatures found, exiting...", ConsoleColor.DarkYellow);
                            break;
                    }
                    IO.Exit();
                }
                
                if (isCompiled)
                {
                    if (!ignoreSignature)
                        (bitsInWord, bitsInAddress) = Signature.CheckSignature(binary);
                    addressType = GetAddressingType();
                    wordType = BitTypes.GetSignedTypeByWidth(bitsInWord);
                    RunInVirtualMachine(
                        wordType,
                        addressType,
                        binary,
                        memoryCapacity,
                        monitorByte: monitorByte,
                        debugExitCode: debugExitCode);
                    IO.Exit(0);
                }

                text = Encoding.UTF8.GetString(binary);
            }
            catch (Exception ex)
            {
                IO.ArgumentError($"Unable to read file.\n{ex}");
            }

            wordType = BitTypes.GetSignedTypeByWidth(bitsInWord);
            addressType = GetAddressingType();
            Type genericCompilerType = typeof(Compiler<,>).MakeGenericType(wordType, addressType);
            object compilerInstance = Activator.CreateInstance(genericCompilerType)!;
            MethodInfo compileMethod = genericCompilerType.GetMethod("Compile")!;
            byte[] binaryImage = (byte[])compileMethod.Invoke(
                compilerInstance,
                [text, emitDebugInfo])!;

            if (!fileless)
                File.WriteAllBytes(output, binaryImage);

            if (runAfter)
                RunInVirtualMachine(
                    wordType,
                    addressType,
                    binaryImage,
                    memoryCapacity,
                    monitorByte: monitorByte,
                    debugExitCode: debugExitCode);

            IO.Exit();

            Type GetAddressingType()
            {
                Type addressType = bitsInAddress == 0
                    ? BitTypes.GetUnsignedTypeByWidth(bitsInWord)
                    : BitTypes.GetUnsignedTypeByWidth(bitsInAddress);
                IO.Log($"Set addressing type to {addressType.Name} from input of {bitsInAddress} bits", null);
                return addressType;
            }

            void RunInVirtualMachine(
                Type wordType,
                Type addressType,
                byte[] binaryImage,
                int memoryCapacity,
                bool disableInterrupts = false,
                TimeSpan? timeout = null,
                long? monitorByte = null,
                int? debugExitCode = null)
            {
                if (monitorLabel != null)
                {
                    if (string.IsNullOrEmpty(input))
                        IO.ArgumentError("No input file provided.");
                    else
                    {
                        try
                        {
                            Label[] definedLabels = DebugInfo.DecodeDebugInfoFromImage(binaryImage);
                            monitorByte = definedLabels.FirstOrDefault(label => label.Name == monitorLabel).Offset;
                        }
                        catch (Exception ex)
                        {
                            IO.ArgumentError($"Debug information is missing or invalid.\n({ex})");
                        }
                    }
                }
                
                VirtualMachineInit.RunInVirtualMachine(
                    wordType,
                    addressType,
                    binaryImage,
                    memoryCapacity,
                    monitorByte: monitorByte,
                    debugExitCode: debugExitCode);
            }

            // VirtualMachine vm = new();
            // 
            // sbyte[] program2 =
            // [
            //     0, 0, 5,    // subleq 0, 0, 7
            //     55, 0,      // db 55, 0
            //     3, 4, 8,    // subleq 5, 6, 10
            //     0, 0, -1,   // subleq 0, 0, -1 ; halt
            //     0, 0, 0     // db 0, 0, 0      ; $t, $e and $r
            // ];
            // 
            // vm.LoadImageAndRun(program2);
            // 
            // IO.Print(vm, ConsoleColor.Yellow);
        }
    }
}