using NetTopologySuite.Geometries;

namespace NetTopologySuite.IO
{
    using Internal;

    public sealed class ShapefileHeaderNG
    {
        private ShapefileHeaderStruct _data;

        public ShapefileHeaderNG(int fileLengthInBytes, ShapeTypeNG shapeType, Envelope boundingBox)
            : this(fileLengthInBytes, shapeType, boundingBox.MinX, boundingBox.MinY, boundingBox.MaxX, boundingBox.MaxY, double.NaN, double.NaN, double.NaN, double.NaN)
        {
        }

        public ShapefileHeaderNG(int fileLengthInBytes, ShapeTypeNG shapeType, double minX, double minY, double maxX, double maxY, double minZ, double maxZ, double minM, double maxM)
        {
            int bigEndianFileLengthInWords = ShapefilePrimitiveHelpers.NativeByteCountToBigEndianWordCount(fileLengthInBytes);
            _data = new ShapefileHeaderStruct(bigEndianFileLengthInWords, shapeType, minX, minY, maxX, maxY, minZ, maxZ, minM, maxM);
        }

        internal ShapefileHeaderNG(in ShapefileHeaderStruct data)
        {
            _data = data;
        }

        public ref int BigEndianFileLengthInWords => ref _data.BigEndianFileLengthInWords;

        public int FileLengthInBytes => _data.FileLengthInBytes;

        public ref ShapeTypeNG ShapeType => ref _data.ShapeType;

        public ref double MinX => ref _data.MinX;

        public ref double MinY => ref _data.MinY;

        public ref double MaxX => ref _data.MaxX;

        public ref double MaxY => ref _data.MaxY;

        public ref double MinZ => ref _data.MinZ;

        public ref double MaxZ => ref _data.MaxZ;

        public ref double MinM => ref _data.MinM;

        public ref double MaxM => ref _data.MaxM;

        internal ref ShapefileHeaderStruct Data => ref _data;
    }
}
