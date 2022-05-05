using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetTopologySuite.IO.Shapefile.Core
{

    /// <summary>
    /// MultiPoint SHP file reader. 
    /// </summary>
    public class ShpMultiPointReader : ShpReader
    {
        /// <inheritdoc/>
        public ShpMultiPointReader(Stream shpStream) : base(shpStream)
        {
            if (!ShapeType.IsMultiPoint())
                throw GetUnsupportedShapeTypeException();
        }

        internal override void ReadShape(BinaryBufferReader shapeBinary)
        {
            shapeBinary.AdvancePastXYBoundingBox();
            var pointCount = shapeBinary.ReadPointCount();

            shapeBinary.ReadPoints(pointCount, HasZ, HasM, Shape);
        }
    }


}
