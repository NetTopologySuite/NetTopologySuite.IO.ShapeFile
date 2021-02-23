using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Dbf;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetTopologySuite.IO.Shapefile
{

    /// <summary>
    /// MultiPolygon shapefile writer.
    /// </summary>
    public class ShapefilePolygonWriter : ShapefileWriter
    {
        /// <inheritdoc/>
        public ShapefilePolygonWriter(string shpPath, ShapeType type, IReadOnlyList<DbfField> fields, Encoding encoding, string projection)
            : base(shpPath, type, fields, encoding, projection)
        { }

        /// <inheritdoc/>
        public ShapefilePolygonWriter(string shpPath, ShapeType type, params DbfField[] fields)
            : base(shpPath, type, fields)
        { }


        internal override Core.ShapefileWriter CreateWriter(string shpPath, ShapeType type, IReadOnlyList<DbfField> fields, Encoding encoding, string projection)
        {
            return new Core.ShapefileMultiPartWriter(shpPath, type, fields, encoding, projection);
        }

        internal override void GetShape(Geometry geometry, Core.ShpShapeBuilder shape)
        {
            shape.Clear();
            if (geometry is MultiPolygon multiPolygon)
            {
                AddMultiPolygon(multiPolygon, shape);
            }
            else if (geometry is Polygon polygon)
            {
                AddPolygon(polygon, shape);
            }
            else
            {
                throw GetUnsupportedGeometryTypeException(geometry);
            }
        }


        private void AddMultiPolygon(MultiPolygon multiPolygon, Core.ShpShapeBuilder shape)
        {
            shape.Clear();

            for (int i = 0; i < multiPolygon.NumGeometries; i++)
            {
                AddPolygon(multiPolygon.GetGeometryN(i) as Polygon, shape);
            }
        }


        private void AddPolygon(Polygon polygon, Core.ShpShapeBuilder shape)
        {
            if (polygon == null || polygon == Polygon.Empty)
                return;

            // SHP Spec: Vertices for a single polygon are always in clockwise order.
            var shellCoordinates = polygon.Shell.IsCCW ? polygon.Shell.CoordinateSequence.Reversed() : polygon.Shell.CoordinateSequence;
            shape.AddPart(shellCoordinates);

            foreach (var hole in polygon.Holes)
            {
                // SHP Spec: Vertices of rings defining holes in polygons are in a counterclockwise direction.
                var holeCoordinates = hole.IsCCW ? hole.CoordinateSequence : hole.CoordinateSequence.Reversed();
                shape.AddPart(holeCoordinates);
            }
        }
    }
}
