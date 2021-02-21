using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Dbf;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetTopologySuite.IO.Shapefile
{

    /// <summary>
    /// MultiPoint shapefile writer.
    /// </summary>
    public class ShapefileMultiPointWriter : ShapefileWriter
    {
        /// <inheritdoc/>
        public ShapefileMultiPointWriter(string shpPath, ShapeType type, IReadOnlyList<DbfField> fields, Encoding encoding, string projection)
            : base(shpPath, type, fields, encoding, projection)
        { }

        /// <inheritdoc/>
        public ShapefileMultiPointWriter(string shpPath, ShapeType type, params DbfField[] fields)
            : base(shpPath, type, fields)
        { }


        internal override Core.ShapefileWriter CreateWriter(string shpPath, ShapeType type, IReadOnlyList<DbfField> fields, Encoding encoding, string projection)
        {
            return new Core.ShapefileMultiPointWriter(shpPath, type, fields, encoding, projection);
        }

        internal override void GetShape(Geometry geometry, Core.ShpShapeBuilder shape)
        {
            shape.Clear();;
            if (geometry is MultiPoint multiPoint)
            {
                shape.StartNewPart();
                AddMultiPoint(shape, multiPoint);
            }
            else
            {
                throw GetUnsupportedGeometryTypeException(geometry);
            }
        }


        private void AddMultiPoint(Core.ShpShapeBuilder shape, MultiPoint multiPoint)
        {
            for (int i = 0; i < multiPoint.NumGeometries; i++)
            {
                shape.AddPoint(multiPoint.GetGeometryN(i) as Point);
            }
        }
    }
}
