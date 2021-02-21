using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NetTopologySuite.IO.Shapefile
{
    /// <summary>
    /// MultiLine shapefile reader.
    /// </summary>
    public class ShapefilePointReader : ShapefileReader
    {

        /// <inheritdoc/>
        internal ShapefilePointReader(string shpPath, Encoding encoding = null) : base(shpPath, encoding)
        { }


        internal override Core.ShapefileReader CreateReader(string shpPath, Encoding encoding)
        {
            return new Core.ShapefilePointReader(shpPath, encoding);
        }


        /// <inheritdoc/>
        public override bool Read(out Geometry geometry, out AttributesTable attributes, out bool deleted)
        {
            var readSucceed = ReadPoint(out var point, out attributes, out deleted);
            geometry = point;
            return readSucceed;
        }


        /// <summary>
        /// Reads <see cref="Point"/> geometry and feature attributes from underlying SHP and DBF files. 
        /// </summary>
        /// <param name="geometry">Feature geometry.</param>
        /// <param name="attributes">Feature atrributes.</param>
        /// <param name="deleted">Indicates if the record was marked as deleted.</param>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next record;
        /// false if the enumerator has passed the end of the table.
        /// </returns>
        public bool ReadPoint(out Point geometry, out AttributesTable attributes, out bool deleted)
        {
            if (!Reader.Read(out deleted))
            {
                geometry = null;
                attributes = null;
                return false;
            }

            geometry = GetPoint(Reader.Shape, HasZ, HasM);
            attributes = Reader.Fields.GetAttributesTable();
            return true;
        }


        internal static Point GetPoint(Core.ShpShapeBuilder shape, bool hasZ, bool hasM)
        {
            if (shape.PointCount < 1)
                return Point.Empty;

            Debug.Assert(shape.PointCount == 1, "Point " + nameof(Core.ShpShapeBuilder) + " has more than one point.");

            return shape[0].ToPoint(hasZ, hasM);
        }
    }
}
