using System.Collections.Generic;
using System.IO;
using NetTopologySuite.Geometries;

namespace NetTopologySuite.IO.Handlers
{
    public class NullShapeHandler : ShapeHandler
    {
        public NullShapeHandler()
            : base(ShapeGeometryType.NullShape)
        {
        }

        public override Geometry Read(BigEndianBinaryReader file,
            int totalRecordLength, GeometryFactory factory)
        {
            return null;
        }

        public override void Write(Geometry geometry, BinaryWriter writer, GeometryFactory factory)
        { }

        public override int ComputeRequiredLengthInWords(Geometry geometry)
        {
            return -1;
        }

        public override IEnumerable<MBRInfo> ReadMBRs(BigEndianBinaryReader reader)
        {
            reader.Close();
            return new MBRInfo[0];
        }
    }
}
