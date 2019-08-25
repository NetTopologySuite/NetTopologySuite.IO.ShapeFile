using NetTopologySuite.Geometries;

namespace NetTopologySuite.IO.Handlers
{
    internal class ShapeMBRIterator : ShapeMBREnumeratorBase
    {
        public ShapeMBRIterator(BigEndianBinaryReader reader)
            : base(reader)
        { }

        protected override Envelope ReadCurrentEnvelope(out int numOfBytesRead)
        {
            double xMin = Reader.ReadDouble();
            double yMin = Reader.ReadDouble();
            double xMax = Reader.ReadDouble();
            double yMax = Reader.ReadDouble();

            numOfBytesRead = 8 * 4;

            return new Envelope(x1: xMin, x2: xMax, y1: yMin, y2: yMax);
        }
    }
}
