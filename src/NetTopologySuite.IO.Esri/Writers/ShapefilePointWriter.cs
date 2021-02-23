using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Dbf;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetTopologySuite.IO.Shapefile
{

    /// <summary>
    /// Point shapefile writer.
    /// </summary>
    public class ShapefilePointWriter : ShapefileWriter
    {
        /// <inheritdoc/>
        public ShapefilePointWriter(string shpPath, ShapeType type, IReadOnlyList<DbfField> fields, Encoding encoding = null, string projection = null)
            : base(shpPath, type, fields, encoding, projection)
        { }

        /// <inheritdoc/>
        public ShapefilePointWriter(string shpPath, ShapeType type, params DbfField[] fields)
            : base(shpPath, type, fields)
        { }


        internal override Core.ShapefileWriter CreateWriter(string shpPath, ShapeType type, IReadOnlyList<DbfField> fields, Encoding encoding, string projection)
        {
            return new Core.ShapefilePointWriter(shpPath, type, fields, encoding, projection);
        }

        internal override void GetShape(Geometry geometry, Core.ShpShapeBuilder shape)
        {
            shape.Clear();
            if (geometry is Point point)
            {
                shape.StartNewPart();
                AddPoint(shape, point);
            }
            else
            {
                throw GetUnsupportedGeometryTypeException(geometry);
            }
        }


        private void AddPoint(Core.ShpShapeBuilder shape, Point point)
        {
            shape.AddPoint(point);
        }
    }
}
