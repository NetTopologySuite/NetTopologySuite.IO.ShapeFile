using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetTopologySuite.IO.Shapefile.Core
{

    /// <summary>
    /// MultiPoint SHP file writer. 
    /// </summary>
    public class ShpMultiPointWriter : ShpWriter
    {
        /// <inheritdoc/>
        public ShpMultiPointWriter(Stream shpStream, Stream shxStream, ShapeType type) : base(shpStream, shxStream, type)
        {
            if (!ShapeType.IsMultiPoint())
                throw GetUnsupportedShapeTypeException();
        }

        internal override bool IsNull(ShpShapeBuilder shape)
        {
            return shape == null || shape.PointCount < 1;
        }

        internal override void WriteShapeToBinary(BinaryBufferWriter shpRecordBinary)
        {
            shpRecordBinary.WriteXYBoundingBox(Shape.Extent);
            shpRecordBinary.WritePointCount(Shape.PointCount);
            shpRecordBinary.WritePoints(HasZ, HasM, Shape);

            Extent.Expand(Shape.Extent);
        }
    }


}
