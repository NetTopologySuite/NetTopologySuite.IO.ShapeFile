using System.Runtime.InteropServices;

namespace NetTopologySuite.IO
{
    using Internal;

    [StructLayout(LayoutKind.Sequential)]
    public struct ShapefileIndexFileRecordNG
    {
        public int BigEndianRecordHeaderOffsetInWords;

        public int BigEndianRecordContentLengthInWords;

        public int RecordHeaderOffsetInBytes
        {
            get => ShapefilePrimitiveHelpers.BigEndianWordCountToNativeByteCount(BigEndianRecordHeaderOffsetInWords);
            set => BigEndianRecordHeaderOffsetInWords = ShapefilePrimitiveHelpers.NativeByteCountToBigEndianWordCount(value);
        }

        public int RecordContentLengthInBytes
        {
            get => ShapefilePrimitiveHelpers.BigEndianWordCountToNativeByteCount(BigEndianRecordContentLengthInWords);
            set => BigEndianRecordContentLengthInWords = ShapefilePrimitiveHelpers.NativeByteCountToBigEndianWordCount(value);
        }
    }
}
