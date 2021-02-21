using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetTopologySuite.IO.Shapefile.Core
{

    /// <summary>
    /// Polygon or MultiPart SHP file reader. 
    /// </summary>
    public class ShpMultiPartReader : ShpReader
    {
        /// <inheritdoc/>
        public ShpMultiPartReader(Stream shpStream) : base(shpStream)
        {
            if (!ShapeType.IsPolygon() && !ShapeType.IsPolyLine())
                throw GetUnsupportedShapeTypeException();
        }

        internal override void ReadShape(BinaryBufferReader shapeBinary)
        {
            shapeBinary.AdvancePastXYBoundingBox();
            var partCount = shapeBinary.ReadPartCount();
            var pointCount = shapeBinary.ReadPointCount();

            shapeBinary.ReadPartOfsets(partCount, Shape);
            shapeBinary.ReadPoints(pointCount, HasZ, HasM, Shape);
        }
    }


}
