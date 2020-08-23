using System;

namespace NetTopologySuite.IO
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Internal;
    using NetTopologySuite.IO.ShapeRecords;

    public ref struct ShapefileSpanReaderNG
    {
        private readonly ShapefileFormatSpanReader _innerReader;

        public ShapefileSpanReaderNG(ReadOnlySpan<byte> mainFile, ReadOnlySpan<byte> indexFile)
        {
            _innerReader = new ShapefileFormatSpanReader(mainFile, indexFile);
            _innerReader.ValidateFileStructure();
            RecordCount = _innerReader.RecordCount;
        }

        public int RecordCount { get; }

        public ShapeTypeNG ShapeType => _innerReader.MainFileHeader.ShapeType;

        public PointXYRecordNG GetPointXYRecord(int recordIndex)
        {
            if ((uint)recordIndex >= (uint)RecordCount)
            {
                ThrowArgumentOutOfRangeExceptionForRecordIndex();
            }

            if (ShapeType != ShapeTypeNG.Point)
            {
                ThrowInvalidOperationExceptionForShapeTypeMismatch();
            }

            return MemoryMarshal.Read<PointXYRecordNG>(_innerReader.GetInnerRecordContents(recordIndex));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentOutOfRangeExceptionForRecordIndex()
        {
            throw new ArgumentOutOfRangeException("recordIndex", "Must be non-negative and less than RecordCount.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowInvalidOperationExceptionForShapeTypeMismatch()
        {
            throw new InvalidOperationException($"This method does not support shapefiles whose ShapeType is {ShapeType}.");
        }
    }
}