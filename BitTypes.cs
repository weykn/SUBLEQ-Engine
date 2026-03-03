using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OneInsArch
{
    public static class BitTypes
    {
        public static readonly byte[] LegalBitTypes = [8, 16, 32, 64, 128];

        public static object CastBitType<T>(long number, int line)
            where T : INumber<T>
        {
            object casted = typeof(T) switch
            {
                var t when t == typeof(byte) => unchecked((byte)number),
                var t when t == typeof(sbyte) => unchecked((sbyte)number),
                var t when t == typeof(short) => unchecked((short)number),
                var t when t == typeof(ushort) => unchecked((ushort)number),
                var t when t == typeof(int) => unchecked((int)number),
                var t when t == typeof(uint) => unchecked((uint)number),
                var t when t == typeof(long) => unchecked((long)number),
                var t when t == typeof(ulong) => unchecked((ulong)number),
                var t when t == typeof(Int128) => unchecked((Int128)number),
                var t when t == typeof(UInt128) => unchecked((UInt128)number),
                _ => IO.CodeError($"Unsupported type for cast: {typeof(T).Name}", line)
            };
            return casted;
        }

        public static Type GetUnsignedTypeByWidth(byte bits)
        {
            Type wordType = bits switch
            {
                8 => typeof(byte),
                16 => typeof(ushort),
                32 => typeof(uint),
                64 => typeof(ulong),
                128 => typeof(UInt128),
                _ => (Type)IO.ArgumentError($"Unsupported size: {bits} bits")
            };
            return wordType;
        }

        public static Type GetSignedTypeByWidth(byte bitsInWord)
        {
            Type wordType = bitsInWord switch
            {
                8 => typeof(sbyte),
                16 => typeof(short),
                32 => typeof(int),
                64 => typeof(long),
                128 => typeof(Int128),
                _ => (Type)IO.ArgumentError($"Unsupported default word size: {bitsInWord} bits")
            };
            return wordType;
        }

        public static sbyte[] ToSBytes(dynamic value)
        {
            Type t = value.GetType();
            byte[] bytes = t switch
            {
                Type _ when t == typeof(byte) => [(byte)value],
                Type _ when t == typeof(sbyte) => [(byte)(sbyte)value],
                Type _ when t == typeof(short) => BitConverter.GetBytes((short)value),
                Type _ when t == typeof(ushort) => BitConverter.GetBytes((ushort)value),
                Type _ when t == typeof(int) => BitConverter.GetBytes((int)value),
                Type _ when t == typeof(uint) => BitConverter.GetBytes((uint)value),
                Type _ when t == typeof(long) => BitConverter.GetBytes((long)value),
                Type _ when t == typeof(ulong) => BitConverter.GetBytes((ulong)value),
                Type _ when t == typeof(float) => BitConverter.GetBytes((float)value),
                Type _ when t == typeof(double) => BitConverter.GetBytes((double)value),
                Type _ when t == typeof(Int128) => BitConverter.GetBytes((Int128)value),
                Type _ when t == typeof(UInt128) => BitConverter.GetBytes((UInt128)value),
                _ => throw new Exception($"Unsupported type: {t.Name}")
            };
            sbyte[] sbytes = new sbyte[bytes.Length];
            Buffer.BlockCopy(bytes, 0, sbytes, 0, bytes.Length);
            return sbytes;
        }
    }
}
