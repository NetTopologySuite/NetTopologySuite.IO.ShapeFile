using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace NetTopologySuite.IO
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ShapefileMainFileRecordHeaderNG
    {
        public int BigEndianRecordNumber;

        public int BigEndianRecordContentLengthInWords;

        public int RecordNumber
        {
            get => ShapefilePrimitiveHelpers.SwapByteOrderIfLittleEndian(BigEndianRecordNumber);
            set => BigEndianRecordNumber = ShapefilePrimitiveHelpers.SwapByteOrderIfLittleEndian(value);
        }

        public uint RecordContentLengthInBytes
        {
            get => ShapefilePrimitiveHelpers.BigEndianWordCountToNativeByteCount(BigEndianRecordContentLengthInWords);
            set => BigEndianRecordContentLengthInWords = ShapefilePrimitiveHelpers.NativeByteCountToBigEndianWordCount(value);
        }
    }
}
