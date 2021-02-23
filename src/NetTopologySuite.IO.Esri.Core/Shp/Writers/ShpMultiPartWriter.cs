using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetTopologySuite.IO.Shapefile.Core
{

    /// <summary>
    /// Polygon or MultiPart SHP file writer. 
    /// </summary>
    public class ShpMultiPartWriter : ShpWriter
    {
        /// <inheritdoc/>
        public ShpMultiPartWriter(Stream shpStream, Stream shxStream, ShapeType type) : base(shpStream, shxStream, type)
        {
            if (!ShapeType.IsPolygon() && !ShapeType.IsPolyLine())
                throw GetUnsupportedShapeTypeException();
        }

        internal override bool IsNull(ShpShapeBuilder shape)
        {
            return shape == null || shape.PartCount < 1 || shape.PointCount < 2;
        }

        internal override void WriteShapeToBinary(BinaryBufferWriter shpRecordBinary)
        {
            shpRecordBinary.WriteXYBoundingBox(Shape.Extent);
            shpRecordBinary.WritePartCount(Shape.PartCount);
            shpRecordBinary.WritePointCount(Shape.PointCount);

            shpRecordBinary.WritePartOffsets(Shape);
            shpRecordBinary.WritePoints(HasZ, HasM, Shape);

            Extent.Expand(Shape.Extent);
        }
    }


}
