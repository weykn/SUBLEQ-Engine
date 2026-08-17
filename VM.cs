using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OneInsArch
{
    public class VirtualMachine<TSigned, TAddress>(
        int memoryCapacity,
        bool disableInterrupts)
        where TSigned : unmanaged, INumber<TSigned>
        where TAddress : unmanaged, INumber<TAddress>
    {
        private readonly bool DisableInterrupts = disableInterrupts;
        private int? DebugExitCode = null;
        public sbyte[] Memory = new sbyte[memoryCapacity];
        private bool StopRequested;

        public override string ToString() => Numeric.FormatSBytes(Memory);

        private static int GetSizeOf<T>()
            where T : unmanaged
            => Unsafe.SizeOf<T>();

        private T Read<T>(int offset)
            where T : unmanaged
            => MemoryMarshal.Read<T>(
                MemoryMarshal.AsBytes(
                    Memory.AsSpan(offset, GetSizeOf<T>())
                )
            );

        private void Write<T>(int offset, T value)
            where T : unmanaged
        {
            MemoryMarshal.Write(
                MemoryMarshal.AsBytes(
                    Memory.AsSpan(offset, GetSizeOf<T>())
                ),
                in value
            );
        }

        private static TSigned Sub(TSigned a, TSigned b)
            => unchecked(a - b);

        private static bool IsLessOrEqualZero(TSigned v)
            => v <= TSigned.Zero;

        private void LoadImage(byte[] binary)
        {
            if (binary.Length > Memory.Length)
                IO.ArgumentError(
                    $"Binary size of {binary.Length} bytes exceeds memory capacity of {Memory.Length} bytes");

            for (int i = 0; i < binary.Length; i++)
                Memory[i] = unchecked((sbyte)binary[i]);
        }

        private void Run()
        {
            int wordSize = GetSizeOf<TSigned>();
            int addrSize = GetSizeOf<TAddress>();
            int instrSize = wordSize * 2 + addrSize;

            for (int ip = 0;
                 ip + instrSize <= Memory.Length && !StopRequested;)
            {
                TSigned a = Read<TSigned>(ip);
                TSigned b = Read<TSigned>(ip + wordSize);
                TAddress c = Read<TAddress>(ip + wordSize * 2);

                int addrA = int.CreateChecked(a);
                int addrB = int.CreateChecked(b);

                TSigned valA = Read<TSigned>(addrA);
                TSigned valB = Read<TSigned>(addrB);

                TSigned result = Sub(valB, valA);
                Write(addrB, result);

                bool condition = IsLessOrEqualZero(result);

                IO.Log($"{ip}: B([{addrB}]={valB}) - A([{addrA}]={valA}) = {result}={condition} goto C({c}) @ subleq {a}, {b}, {c}", null);

                if (condition)
                {
                    ip = int.CreateChecked(c);
                    if (DebugExitCode != ip) continue;
                    IO.Log("Debug exit code triggered.", null);
                    StopRequested = true;
                }
                else
                {
                    ip += instrSize;
                }
            }
        }

        private void CallRun()
        {
            Run();
            IO.Log(ToString(), null);
        }

        public void LoadImageAndRun(
            byte[] binary,
            TimeSpan? timeout = null,
            long[]? monitorBytes = null,
            int? debugExitCode = null)
        {
            DebugExitCode = debugExitCode;
            
            LoadImage(binary);

            if (timeout == null)
            {
                CallRun();
            }
            else if (!Task.Run(CallRun).Wait(timeout.Value))
            {
                StopRequested = true;
                throw new TimeoutException(
                    $"Virtual machine timed out after {timeout.Value.TotalSeconds:F1}s");
            }

            if (monitorBytes == null) return;
            
            foreach (long i in monitorBytes)
                IO.Print($"State of byte {i} is {Memory[i]}.");
        }
    }
}
