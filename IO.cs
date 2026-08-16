#pragma warning disable IDE0079
#pragma warning disable CA2211

using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection.Emit;
using System.Security;
using System.Text;

namespace OneInsArch
{
    public static class IO
    {
        private class Text(object? value,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor,
            bool newLine,
            int? line,
            bool preLine,
            string atLineText)
        {
            public static List<Text> Todo = [];

            public object? Value = value;
            public ConsoleColor ForegroundColor = foregroundColor;
            public ConsoleColor BackgroundColor = backgroundColor;
            public bool NewLine = newLine;
            public int? Line = line;
            public bool PreLine = preLine;
            public string AtLineText = atLineText;

            public static void Add(Text value) => Todo.Add(value);

            public void Print()
            {
                Console.ForegroundColor = ForegroundColor;
                Console.BackgroundColor = BackgroundColor;
                if (PreLine) WriteLine();
                if (Line != null) Write($"{AtLineText}{Line}: ");
                if (NewLine) WriteLine(Value);
                else Write(Value);
            }

            public static void WriteLine(object? text = null) => Write(text + Environment.NewLine);

            private static void Write(object? text)
            {
                if (Rainbow) RainbowPrinter.WriteRainbow(text?.ToString() ?? string.Empty);
                else Console.Write(text);
            }

            public static void PrintTodo()
            {
                foreach (Text text in Todo)
                    text.Print();
            }
        }

        public static bool Debug;
        public static bool InactivePrint;
        public static bool Rainbow;
        public static bool ThrowOnCodeError;

        public static string FormatElapsed(double seconds)
        {
            if (seconds < 1e-3) return $"{seconds * 1e6:0.###} µs";
            if (seconds < 1) return $"{seconds * 1e3:0.###} ms";
            if (seconds < 60) return $"{seconds:0.###} s";
            if (seconds < 3600) return $"{seconds / 60:0.###} min";
            return $"{seconds / 3600:0.###} h";
        }

        public static void ShowHelp()
        {
            Print($@"Usage: {Signature.GetFileName()} [options] <input file>
                                           
General Options:                                   
  -h, --help                               Show this help message and exit. 
  -v, --version                            Display version information.
                                                   
Compiler Options:                                  
  -o, --output <file>                      Specify the output file name.
  -w, --word <bits>                        Specify the default word size in bits (default: {Signature.CurrentSignature.WordSize}).
  -f, --fileless                           Compile and run without writing an output file.
      --address <bits>                     Force addressing size in bits.
      --speed-up                           Faster compilation, higher memory usage.
      --flow-protection                    Prevent return-hijacking via shadow stack.
                                                   
Virtual Machine Options:                           
  -m, --memory <bytes>                     Set virtual machine memory capacity in bytes (default: {VirtualMachineInit.DefaultMemoryCapacity}).
  -r, --run                                Run program in virtual machine.
                                                   
Binary Options:                                    
  -i, --info                               Display information about the binary and exit.
                                                   
Debug/Developer Options:                           
  -g, --debug                              Include debug symbols in the generated binary.
      --verbose                            Enable verbose logging.
      --try-sign                           Attempt to find a signature to an unsigned file.
      --no-signature                       Do not append the signature/metadata.
      --read-signature                     Print signature information and exit.
      --ignore-signature                   Ignore binary signature when loading.
      --read-debug                         Display debug symbols from binary.
      --debug-monitor <label/address>      Print the last state of a byte by label or address.
      --debug-exit-code <code>             Exit virtual machine on code.
      --rainbow                            Enable rainbow text.", ConsoleColor.DarkCyan);
        }

        public static void PrintTodo() => Text.PrintTodo();

        public static void Print(
            object? value,
            ConsoleColor foregroundColor = ConsoleColor.White,
            ConsoleColor backgroundColor = ConsoleColor.Black,
            bool newLine = true,
            int? line = null,
            bool preLine = false,
            string atLineText = "At line ")
        {
            Text text = new(
                value,
                foregroundColor,
                backgroundColor,
                newLine,
                line,
                preLine,
                atLineText);
            if (InactivePrint) Text.Add(text);
            else text.Print();
        }

        public static void Exit(int code = 0)
        {
            PrintTodo();
            Console.ResetColor();
            Environment.Exit(code);
        }

        public static void Warning(string message)
        {
            Print("Warning: " + message, ConsoleColor.Black, ConsoleColor.DarkYellow);
        }

        public static object ArgumentError(string message)
        {
            Print(message, ConsoleColor.Black, ConsoleColor.Red);
            ShowHelp();
            Exit(1);
            return 0;
        }

        public static object Unassigned(Label label, int line)
        {
            CodeError($"Tried to reference unassigned variable {label.Name}", line);
            return 0;
        }

        public static object CodeError(
            string message,
            int line,
            string? fullLine = null,
            int? charIndex = null,
            int? len = null)
        {
            if (ThrowOnCodeError)
            {
                throw new Exception($"Code error at line {line} {message}");
            }
            else
            {
                Log($"CODE ERROR {{\"message\":\"{message}\",\"line\":{line},\"fullLine\":\"{fullLine}\",\"charIndex\":{charIndex},\"len\":{len}}}", line);
                if (fullLine != null)
                {
                    string underline = new string(
                        ' ', (int)charIndex!) + "^" + new string('~',
                        Math.Max(0, (byte)len! - 1));
                    message += $"\n {fullLine}\n {underline}";
                }
                Print(
                    message,
                    ConsoleColor.Red,
                    ConsoleColor.Black,
                    line: line,
                    preLine: true,
                    atLineText: "Error at line ");
                Exit(2);
                return 0;
            }
        }

        public static void EngineError(Exception ex)
        {
            Print($"An unhandled error occured, report the following: {ex}", ConsoleColor.White, ConsoleColor.DarkRed);
            Exit(3);
        }

        public static void Log(string? value, int? line, bool newLine = true)
        {
            if (Debug) Print(value, ConsoleColor.Cyan, newLine: newLine, line: line);
        }
    }
}
