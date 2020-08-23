using System;
using System.IO;
using System.Runtime.InteropServices;

namespace NetTopologySuite.IO.Internal
{
    internal class ShapefileFormatForwardOnlyReader
    {
        private readonly Stream _forwardOnlyReadableStream;

        private readonly byte[] _oneHundredByteBuffer = new byte[100];

        public ShapefileFormatForwardOnlyReader(Stream forwardOnlyReadableStream)
        {
            _forwardOnlyReadableStream = forwardOnlyReadableStream;
        }

        public ShapefileHeaderNG ReadHeader()
        {
            byte[] scratchBuffer = _oneHundredByteBuffer;
            GeneralIOHelpers.FillBufferOrThrow(_forwardOnlyReadableStream, scratchBuffer, 0, 100);

            ReadOnlySpan<byte> headerBytes = scratchBuffer;
            int bigEndianFileLengthInWords = MemoryMarshal.Read<int>(headerBytes.Slice(24, 4));
            var shapeType = MemoryMarshal.Read<ShapeTypeNG>(headerBytes.Slice(32, 4));
            var boundingBox = MemoryMarshal.Cast<byte, double>(headerBytes.Slice(36));
            return new ShapefileHeaderNG(bigEndianFileLengthInWords, shapeType, boundingBox[0], boundingBox[1], boundingBox[2], boundingBox[3], boundingBox[4], boundingBox[5], boundingBox[6], boundingBox[7]);
        }

        public ShapefileIndexFileRecordNG ReadIndexFileRecordHeader()
        {
            byte[] scratchBuffer = _oneHundredByteBuffer;
            GeneralIOHelpers.FillBufferOrThrow(_forwardOnlyReadableStream, scratchBuffer, 0, 8);
            return MemoryMarshal.Read<ShapefileIndexFileRecordNG>(scratchBuffer);
        }

        public ShapefileMainFileRecordHeaderNG ReadMainFileRecordHeader()
        {
            byte[] scratchBuffer = _oneHundredByteBuffer;
            GeneralIOHelpers.FillBufferOrThrow(_forwardOnlyReadableStream, scratchBuffer, 0, 8);
            return MemoryMarshal.Read<ShapefileMainFileRecordHeaderNG>(scratchBuffer);
        }

        public void FillNextRecordContents(byte[] buffer, int offset, int count)
        {
            GeneralIOHelpers.FillBufferOrThrow(_forwardOnlyReadableStream, buffer, offset, count);
        }
    }
}
