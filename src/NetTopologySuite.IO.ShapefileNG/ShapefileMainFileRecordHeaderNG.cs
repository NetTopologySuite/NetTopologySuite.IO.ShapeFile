using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace NetTopologySuite.IO
{
    using Internal;

    [StructLayout(LayoutKind.Sequential)]
    public struct ShapefileMainFileRecordHeaderNG
    {
        public int BigEndianRecordNumber;

        public int BigEndianRecordContentLengthInWords;

        public int RecordNumber
        {
            get => ShapefilePrimitiveHelpers.SwapByteOrderOnLittleEndianMachines(BigEndianRecordNumber);
            set => BigEndianRecordNumber = ShapefilePrimitiveHelpers.SwapByteOrderOnLittleEndianMachines(value);
        }

        public int RecordContentLengthInBytes
        {
            get => ShapefilePrimitiveHelpers.BigEndianWordCountToNativeByteCount(BigEndianRecordContentLengthInWords);
            set => BigEndianRecordContentLengthInWords = ShapefilePrimitiveHelpers.NativeByteCountToBigEndianWordCount(value);
        }
    }
}
