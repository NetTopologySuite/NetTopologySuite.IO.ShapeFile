using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetTopologySuite.IO.Shapefile.Core
{

    /// <summary>
    /// Point SHP file reader. 
    /// </summary>
    public class ShpPointReader : ShpReader
    {
        private ShpCoordinates PointBuffer = new ShpCoordinates(0.0, 0.0); // This sets Z=NaN, M=NaN (default constructor sets it to 0.0)

        /// <inheritdoc/>
        public ShpPointReader(Stream shpStream) : base(shpStream)
        {
            if (!ShapeType.IsPoint())
                throw GetUnsupportedShapeTypeException();
        }

        internal override void ReadShape(BinaryBufferReader shapeBinary)
        {
            shapeBinary.ReadPoint(HasZ, HasM, ref PointBuffer);
            base.Shape.AddPoint(PointBuffer.X, PointBuffer.Y, PointBuffer.Z, PointBuffer.M);
        }

        /// <summary>
        /// Point shape.
        /// </summary>
        public ShpCoordinates Point => base.Shape.Points[0];
    }


}
