using System;

namespace NetTopologySuite.IO
{
    internal static class BitTweaks
    {
        internal static int ReverseByteOrder(int value) => unchecked((int)ReverseByteOrder((uint)value));

        internal static long ReverseByteOrder(long value) => unchecked((long)ReverseByteOrder((ulong)value));

        internal static double ReverseByteOrder(double value)
        {
            long bits = BitConverter.DoubleToInt64Bits(value);
            bits = ReverseByteOrder(bits);
            return BitConverter.Int64BitsToDouble(bits);
        }

        internal static uint ReverseByteOrder(uint value) => (value & 0x000000FF) << 24 |
                                                             (value & 0x0000FF00) << 8 |
                                                             (value & 0x00FF0000) >> 8 |
                                                             (value & 0xFF000000) >> 24;

        internal static ulong ReverseByteOrder(ulong value) => (value & 0x00000000000000FF) << 56 |
                                                               (value & 0x000000000000FF00) << 40 |
                                                               (value & 0x0000000000FF0000) << 24 |
                                                               (value & 0x00000000FF000000) << 8 |
                                                               (value & 0x000000FF00000000) >> 8 |
                                                               (value & 0x0000FF0000000000) >> 24 |
                                                               (value & 0x00FF000000000000) >> 40 |
                                                               (value & 0xFF00000000000000) >> 56;
    }
}
