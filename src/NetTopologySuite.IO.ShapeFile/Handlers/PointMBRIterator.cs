using NetTopologySuite.Geometries;

namespace NetTopologySuite.IO.Handlers
{
    internal class PointMBRIterator : ShapeMBREnumeratorBase
    {
        public PointMBRIterator(BigEndianBinaryReader reader)
            : base(reader)
        { }

        protected override Envelope ReadCurrentEnvelope(out int numOfBytesRead)
        {
            double x = Reader.ReadDouble();
            double y = Reader.ReadDouble();

            numOfBytesRead = 16;

            return new Envelope(x, x, y, y);
        }
    }
}
