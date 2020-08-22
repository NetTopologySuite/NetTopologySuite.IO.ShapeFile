using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace NetTopologySuite.IO
{
    internal static class GeneralIOHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FillBufferOrThrow(Stream stream, byte[] buffer, int offset, int count)
        {
            // optimize for the overwhelmingly most likely path: one read does the trick.
            int rd = stream.Read(buffer, offset, count);
            if (rd != count)
            {
                FillBufferOrThrowRare(stream, buffer, offset, count, rd);
            }
        }

        private static void FillBufferOrThrowRare(Stream stream, byte[] buffer, int offset, int count, int prevRead)
        {
            while (prevRead != 0)
            {
                offset += prevRead;
                count -= prevRead;
                prevRead = stream.Read(buffer, offset, count);
                if (prevRead == count)
                {
                    return;
                }
            }

            throw new EndOfStreamException();
        }
    }
}
