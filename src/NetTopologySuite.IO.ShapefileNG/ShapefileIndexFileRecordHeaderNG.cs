using System.Runtime.InteropServices;

namespace NetTopologySuite.IO
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ShapefileIndexFileRecordHeaderNG
    {
        public int BigEndianRecordHeaderOffsetInWords;

        public int BigEndianRecordContentLengthInWords;

        public uint RecordHeaderOffsetInBytes
        {
            get => ShapefilePrimitiveHelpers.BigEndianWordCountToNativeByteCount(BigEndianRecordHeaderOffsetInWords);
            set => BigEndianRecordHeaderOffsetInWords = ShapefilePrimitiveHelpers.NativeByteCountToBigEndianWordCount(value);
        }

        public uint RecordContentLengthInBytes
        {
            get => ShapefilePrimitiveHelpers.BigEndianWordCountToNativeByteCount(BigEndianRecordContentLengthInWords);
            set => BigEndianRecordContentLengthInWords = ShapefilePrimitiveHelpers.NativeByteCountToBigEndianWordCount(value);
        }
    }
}
