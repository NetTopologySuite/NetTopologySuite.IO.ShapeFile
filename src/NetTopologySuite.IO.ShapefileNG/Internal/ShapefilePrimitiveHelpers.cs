using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace NetTopologySuite.IO.Internal
{
    internal static class ShapefilePrimitiveHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidateLittleEndianMachine()
        {
            if (!BitConverter.IsLittleEndian)
            {
                ThrowForBigEndianMachine();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SwapByteOrderOnLittleEndianMachines<T>(T val)
            where T : unmanaged
        {
            ValidateLittleEndianMachine();
            if (typeof(T) == typeof(int) || typeof(T) == typeof(ShapeTypeNG))
            {
                ref int s32 = ref Unsafe.As<T, int>(ref val);
                s32 = BinaryPrimitives.ReverseEndianness(s32);
            }
            else if (typeof(T) == typeof(long) || typeof(T) == typeof(double))
            {
                ref long s64 = ref Unsafe.As<T, long>(ref val);
                s64 = BinaryPrimitives.ReverseEndianness(s64);
            }

            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BigEndianWordCountToNativeByteCount(int val)
        {
            val = SwapByteOrderOnLittleEndianMachines(val);
            return checked(val * 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NativeByteCountToBigEndianWordCount(int val)
        {
            return SwapByteOrderOnLittleEndianMachines(val / 2);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowForBigEndianMachine()
        {
            throw new NotImplementedException("Support for big-endian machine architectures has not yet been implemented.");
        }
    }
}
