using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetTopologySuite.IO.Shapefile.Core
{

    /// <summary>
    /// Point SHP file writer. 
    /// </summary>
    public class ShpPointWriter : ShpWriter
    {
        /// <inheritdoc/>
        public ShpPointWriter(Stream shpStream, Stream shxStream, ShapeType type) : base(shpStream, shxStream, type)
        {
            if (!ShapeType.IsPoint())
                throw GetUnsupportedShapeTypeException();
        }

        internal override bool IsNull(ShpShapeBuilder shape)
        {
            return shape == null || shape.PointCount < 1 || shape.FirstPointIsNull;
        }

        internal override void WriteShapeToBinary(BinaryBufferWriter shpRecordBinary)
        {
            shpRecordBinary.WritePoint(HasZ, HasM, Point);
            Extent.Expand(Shape.Extent);
        }

        /// <summary>
        /// Point shape.
        /// </summary>
        public ShpCoordinates Point
        {
            get { return Shape.Points[0]; }
            set
            {
                Shape.Clear();
                Shape.AddPoint(value); // This updates extent.
            }
        }
    }


}
