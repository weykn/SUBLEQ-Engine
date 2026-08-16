using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OneInsArch
{
    public static class VirtualMachineInit
    {
        public const int DefaultMemoryCapacity = 1024;

        public static object RunInVirtualMachine(
            Type wordType,
            Type addressType,
            byte[] binaryImage,
            int memoryCapacity,
            bool disableInterrupts = false,
            TimeSpan? timeout = null,
            long? monitorByte = null,
            int? debugExitCode = null)
        {
            long[]? monitorBytes = monitorByte == null ? [] : [(long)monitorByte];
            IO.Log($"Running binary image with word type {wordType.Name} and memory capacity of {memoryCapacity} bytes", null);
            Type genericVmType = typeof(VirtualMachine<,>).MakeGenericType(wordType, addressType);
            object vmInstance = Activator.CreateInstance(genericVmType, [memoryCapacity, disableInterrupts])!;
            MethodInfo loadImageAndRunMethod = genericVmType.GetMethod("LoadImageAndRun")!;
            loadImageAndRunMethod.Invoke(
                vmInstance,
                [binaryImage, timeout, monitorBytes, debugExitCode]);
            return vmInstance;
        }
    }
}
