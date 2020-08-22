using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace NetTopologySuite.IO
{
    internal static class ShapefilePrimitiveHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SwapByteOrderIfLittleEndian(int val)
        {
            if (BitConverter.IsLittleEndian)
            {
                val = BinaryPrimitives.ReverseEndianness(val);
            }

            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint BigEndianWordCountToNativeByteCount(int val)
        {
            val = SwapByteOrderIfLittleEndian(val);
            return ((uint)val) * 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint BigEndianWordCountToNativeByteCount(ReadOnlySpan<byte> val)
        {
            return ((uint)BinaryPrimitives.ReadInt32BigEndian(val)) * 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NativeByteCountToBigEndianWordCount(uint val)
        {
            int result = (int)(val / 2);
            return SwapByteOrderIfLittleEndian(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NativeByteCountToBigEndianWordCount(uint val, Span<byte> bytes)
        {
            BinaryPrimitives.WriteInt32BigEndian(bytes, (int)(val / 2));
        }
    }
}
