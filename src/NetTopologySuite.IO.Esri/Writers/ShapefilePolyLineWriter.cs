using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Dbf;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetTopologySuite.IO.Shapefile
{

    /// <summary>
    /// MultiLine shapefile writer.
    /// </summary>
    public class ShapefilePolyLineWriter : ShapefileWriter
    {
        /// <inheritdoc/>
        public ShapefilePolyLineWriter(string shpPath, ShapeType type, IReadOnlyList<DbfField> fields, Encoding encoding, string projection)
            : base(shpPath, type, fields, encoding, projection)
        { }

        /// <inheritdoc/>
        public ShapefilePolyLineWriter(string shpPath, ShapeType type, params DbfField[] fields)
            : base(shpPath, type, fields)
        { }


        internal override Core.ShapefileWriter CreateWriter(string shpPath, ShapeType type, IReadOnlyList<DbfField> fields, Encoding encoding, string projection)
        {
            return new Core.ShapefileMultiPartWriter(shpPath, type, fields, encoding, projection);
        }

        internal override void GetShape(Geometry geometry, Core.ShpShapeBuilder shape)
        {
            shape.Clear();
            if (geometry is MultiLineString multiLine)
            {
                AddMultiLineString(multiLine, shape);
            }
            else if (geometry is LineString line)
            {
                AddLineString(line, shape);
            }
            else
            {
                throw GetUnsupportedGeometryTypeException(geometry);
            }
        }


        private void AddMultiLineString(MultiLineString multiLine, Core.ShpShapeBuilder shape)
        {
            for (int i = 0; i < multiLine.NumGeometries; i++)
            {
                AddLineString(multiLine.GetGeometryN(i) as LineString, shape);
            }
        }


        private void AddLineString(LineString line, Core.ShpShapeBuilder shape)
        {
            if (line == null || line == LineString.Empty)
                return;

            shape.AddPart(line.CoordinateSequence);
        }
    }
}
