using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetTopologySuite.IO.Shapefile
{
    /// <summary>
    /// MultiPoint shapefile reader.
    /// </summary>
    public class ShapefileMultiPointReader : ShapefileReader
    {

        /// <inheritdoc/>
        internal ShapefileMultiPointReader(string shpPath, Encoding encoding = null) : base(shpPath, encoding)
        { }


        internal override Core.ShapefileReader CreateReader(string shpPath, Encoding encoding)
        {
            return new Core.ShapefileMultiPointReader(shpPath, encoding);
        }


        /// <inheritdoc/>
        public override bool Read(out Geometry geometry, out AttributesTable attributes, out bool deleted)
        {
            var readSucceed = ReadMultiPoint(out var multiPoint, out attributes, out deleted);
            geometry = multiPoint;
            return readSucceed;
        }


        /// <summary>
        /// Reads <see cref="MultiPoint"/> geometry and feature attributes from underlying SHP and DBF files. 
        /// </summary>
        /// <param name="geometry">Feature geometry.</param>
        /// <param name="attributes">Feature atrributes.</param>
        /// <param name="deleted">Indicates if the record was marked as deleted.</param>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next record;
        /// false if the enumerator has passed the end of the table.
        /// </returns>
        public bool ReadMultiPoint(out MultiPoint geometry, out AttributesTable attributes, out bool deleted)
        {
            if (!Reader.Read(out deleted))
            {
                geometry = null;
                attributes = null;
                return false;
            }

            geometry = GetMultiPoint(Reader.Shape, HasZ, HasM);
            attributes = Reader.Fields.GetAttributesTable();
            return true;
        }


        internal static MultiPoint GetMultiPoint(Core.ShpShapeBuilder shape, bool hasZ, bool hasM)
        {
            if (shape.PointCount < 1)
                return MultiPoint.Empty;

            var pointCount = shape.PointCount;
            var points = new Point[pointCount];

            for (int i = 0; i < pointCount; i++)
            {
                points[i] = shape[i].ToPoint(hasZ, hasM);
            }

            return new MultiPoint(points);
        }

    }
}
