using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace NetTopologySuite.IO.Internal
{
    internal static class GeneralIOHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FillBufferOrThrow(Stream stream, byte[] buffer, int offset, int count)
        {
            // optimize for the overwhelmingly most likely path: one read does the trick.
            int bytesRead = stream.Read(buffer, offset, count);
            if (bytesRead != count)
            {
                FillBufferOrThrowRare(stream, buffer, offset, count, bytesRead);
            }
        }

        private static void FillBufferOrThrowRare(Stream stream, byte[] buffer, int offset, int count, int prevBytesRead)
        {
            while (prevBytesRead != 0)
            {
                offset += prevBytesRead;
                count -= prevBytesRead;
                prevBytesRead = stream.Read(buffer, offset, count);
                if (prevBytesRead == count)
                {
                    return;
                }
            }

            throw new EndOfStreamException();
        }
    }
}
