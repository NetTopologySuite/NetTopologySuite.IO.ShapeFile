using NetTopologySuite.Geometries;

namespace NetTopologySuite.IO
{
    public sealed class ShapefileHeaderNG
    {
        public ShapefileHeaderNG(uint fileLengthInBytes, ShapeTypeNG shapeType, Envelope boundingBox)
            : this(fileLengthInBytes, shapeType, boundingBox.MinX, boundingBox.MinY, boundingBox.MaxX, boundingBox.MaxY, double.NaN, double.NaN, double.NaN, double.NaN)
        {
        }

        public ShapefileHeaderNG(uint fileLengthInBytes, ShapeTypeNG shapeType, double minX, double minY, double maxX, double maxY, double minZ, double maxZ, double minM, double maxM)
            : this(ShapefilePrimitiveHelpers.NativeByteCountToBigEndianWordCount(fileLengthInBytes), shapeType, minX, minY, maxX, maxY, minZ, maxZ, minM, maxM)
        {
        }

        internal ShapefileHeaderNG(int bigEndianFileLengthInWords, ShapeTypeNG shapeType, double minX, double minY, double maxX, double maxY, double minZ, double maxZ, double minM, double maxM)
        {
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

        public int BigEndianFileLengthInWords { get; }

        public uint FileLengthInBytes => ShapefilePrimitiveHelpers.BigEndianWordCountToNativeByteCount(BigEndianFileLengthInWords);

        public ShapeTypeNG ShapeType { get; }

        public double MinX { get; }

        public double MinY { get; }

        public double MaxX { get; }

        public double MaxY { get; }

        public double MinZ { get; }

        public double MaxZ { get; }

        public double MinM { get; }

        public double MaxM { get; }
    }
}
