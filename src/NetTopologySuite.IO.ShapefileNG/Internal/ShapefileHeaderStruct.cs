using System.Runtime.InteropServices;

namespace NetTopologySuite.IO.Internal
{
    [StructLayout(LayoutKind.Explicit, Size = 100, Pack = 1)]
    internal struct ShapefileHeaderStruct
    {
        private static readonly int BigEndianFileCode = ShapefilePrimitiveHelpers.SwapByteOrderOnLittleEndianMachines(9994);

        [FieldOffset(0)]
        private readonly int _fileCode;

        [FieldOffset(28)]
        private readonly int _version;

        [FieldOffset(24)]
        public int BigEndianFileLengthInWords;

        [FieldOffset(32)]
        public ShapeTypeNG ShapeType;

        [FieldOffset(36)]
        public double MinX;

        [FieldOffset(44)]
        public double MinY;

        [FieldOffset(52)]
        public double MaxX;

        [FieldOffset(60)]
        public double MaxY;

        [FieldOffset(68)]
        public double MinZ;

        [FieldOffset(76)]
        public double MaxZ;

        [FieldOffset(84)]
        public double MinM;

        [FieldOffset(92)]
        public double MaxM;

        public ShapefileHeaderStruct(int bigEndianFileLengthInWords, ShapeTypeNG shapeType, double minX, double minY, double maxX, double maxY, double minZ, double maxZ, double minM, double maxM)
        {
            this = default;
            _fileCode = BigEndianFileCode;
            _version = 1000;

            BigEndianFileLengthInWords = bigEndianFileLengthInWords;
            ShapeType = shapeType;
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
            MinZ = minZ;
            MaxZ = maxZ;
            MinM = minM;
            MaxM = maxM;
        }

        public int FileLengthInBytes
        {
            get => ShapefilePrimitiveHelpers.BigEndianWordCountToNativeByteCount(BigEndianFileLengthInWords);
            set => BigEndianFileLengthInWords = ShapefilePrimitiveHelpers.NativeByteCountToBigEndianWordCount(value);
        }
    }
}