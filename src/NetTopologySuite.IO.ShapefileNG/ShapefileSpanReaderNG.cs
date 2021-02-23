using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;    

namespace NetTopologySuite.IO
{
    using Internal;
    using ShapeRecords;

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

        public MultiPointXYRecordNG GetMultiPointXYRecord(int recordIndex)
        {
            if ((uint)recordIndex >= (uint)RecordCount)
            {
                ThrowArgumentOutOfRangeExceptionForRecordIndex();
            }

            if (ShapeType != ShapeTypeNG.MultiPoint)
            {
                ThrowInvalidOperationExceptionForShapeTypeMismatch();
            }

            var recordContents = _innerReader.GetInnerRecordContents(recordIndex);
            var bbox = MemoryMarshal.Cast<byte, double>(recordContents.Slice(0, 32));
            int numPoints = MemoryMarshal.Read<int>(recordContents.Slice(32, 4));
            var points = MemoryMarshal.Cast<byte, PointXYRecordNG>(recordContents.Slice(36, numPoints * Unsafe.SizeOf<PointXYRecordNG>()));
            return new MultiPointXYRecordNG(bbox[0], bbox[1], bbox[2], bbox[3], points);
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