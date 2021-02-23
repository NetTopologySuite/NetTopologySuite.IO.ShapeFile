using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetTopologySuite.IO.Shapefile
{
    /// <summary>
    /// MultiLine shapefile reader.
    /// </summary>
    public class ShapefilePolyLineReader : ShapefileReader
    {

        /// <inheritdoc/>
        internal ShapefilePolyLineReader(string shpPath, Encoding encoding = null) : base(shpPath, encoding)
        { }

        internal override Core.ShapefileReader CreateReader(string shpPath, Encoding encoding)
        {
            return new Core.ShapefileMultiPartReader(shpPath, encoding);
        }

        /// <inheritdoc/>
        public override bool Read(out Geometry geometry, out AttributesTable attributes, out bool deleted)
        {
            var readSucceed = ReadMultiLine(out var multiLine, out attributes, out deleted);
            if (multiLine.Count == 1)
            {
                geometry = multiLine[0]; // LineString
            }
            else
            {
                geometry = multiLine;  // MultiLineString
            }
            return readSucceed;
        }


        /// <summary>
        /// Reads <see cref="MultiLineString"/> geometry and feature attributes from underlying SHP and DBF files. 
        /// </summary>
        /// <param name="geometry">Feature geometry.</param>
        /// <param name="attributes">Feature atrributes.</param>
        /// <param name="deleted">Indicates if the record was marked as deleted.</param>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next record;
        /// false if the enumerator has passed the end of the table.
        /// </returns>
        public bool ReadMultiLine(out MultiLineString geometry, out AttributesTable attributes, out bool deleted)
        {
            if (!Reader.Read(out deleted))
            {
                geometry = null;
                attributes = null;
                return false;
            }

            geometry = GetMultiLineString(Reader.Shape, HasZ, HasM);
            attributes = Reader.Fields.GetAttributesTable();
            return true;
        }


        internal static MultiLineString GetMultiLineString(Core.ShpShapeBuilder shape, bool hasZ, bool hasM)
        {
            if (shape.PartCount < 1 || shape.PointCount < 2)
                return MultiLineString.Empty;

            var lines = new List<LineString>(shape.PartCount);

            for (int partIndex = 0; partIndex < shape.PartCount; partIndex++)
            {
                var partCoordinates = shape.GetPartCoordinates(partIndex, hasZ, hasM);

                // Line must have at least 2 points
                if (partCoordinates.Count < 2)
                    continue;

                var line = new LineString(partCoordinates, GeometryFactory.Default);
                lines.Add(line);

            }

            if (lines.Count < 1)
                return MultiLineString.Empty;

            return new MultiLineString(lines.ToArray());
        }
    }
}
