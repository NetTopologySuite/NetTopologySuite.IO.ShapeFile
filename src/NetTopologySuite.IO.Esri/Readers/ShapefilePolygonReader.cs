using NetTopologySuite.Algorithm;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetTopologySuite.IO.Shapefile
{
    /// <summary>
    /// MultiPolygon shapefile reader.
    /// </summary>
    public class ShapefilePolygonReader : ShapefileReader
    {
        private static readonly LinearRing[] NoHoles = new LinearRing[0];

        /// <inheritdoc/>
        internal ShapefilePolygonReader(string shpPath, Encoding encoding = null) : base(shpPath, encoding)
        { }


        internal override Core.ShapefileReader CreateReader(string shpPath, Encoding encoding)
        {
            return new Core.ShapefileMultiPartReader(shpPath, encoding);
        }


        /// <inheritdoc/>
        public override bool Read(out Geometry geometry, out AttributesTable attributes, out bool deleted)
        {
            var readSucceed = ReadMultiPolygon(out var multiPolygon, out attributes, out deleted);
            if (multiPolygon.Count == 1)
            {
                geometry = multiPolygon[0]; // Polygon
            }
            else
            {
                geometry = multiPolygon;  // MultiPolygon
            }
            return readSucceed;
        }


        /// <summary>
        /// Reads <see cref="MultiPolygon"/> geometry and feature attributes from underlying SHP and DBF files. 
        /// </summary>
        /// <param name="geometry">Feature geometry.</param>
        /// <param name="attributes">Feature atrributes.</param>
        /// <param name="deleted">Indicates if the record was marked as deleted.</param>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next record;
        /// false if the enumerator has passed the end of the table.
        /// </returns>
        public bool ReadMultiPolygon(out MultiPolygon geometry, out AttributesTable attributes, out bool deleted)
        {
            if (!Reader.Read(out deleted))
            {
                geometry = null;
                attributes = null;
                return false;
            }

            geometry = GetMultiPolygon(Reader.Shape, HasZ, HasM);
            attributes = Reader.Fields.GetAttributesTable();
            return true;
        }



        internal static MultiPolygon GetMultiPolygon(Core.ShpShapeBuilder shape, bool hasZ, bool hasM)
        {
            if (shape.PartCount < 1 || shape.PointCount < 3)
                return MultiPolygon.Empty;

            var (shells, holes) = GetShellsAndHoles(shape, hasZ, hasM);

            if (shells.Count < 1)
                return MultiPolygon.Empty;

            // https://gis.stackexchange.com/a/147971/26684
            // There could be nested shells (nested outer rings). Add a hole to the smallest one. 
            shells.Sort(CompareLinearRingAreas);

            var polygons = new Polygon[shells.Count];
            for (int i = 0; i < shells.Count; i++)
            {
                var shell = shells[i];
                var shellHoles = PopHoles(shell, holes);
                polygons[i] = new Polygon(shell, shellHoles);
            }

            return new MultiPolygon(polygons);
        }


        private static LinearRing[] PopHoles(LinearRing shell, List<LinearRing> holes)
        {
            if (holes.Count < 1)
                return NoHoles;

            var shellHoles = new List<LinearRing>();

            for (int i = holes.Count - 1; i >= 0; i--)
            {
                var hole = holes[i];
                if (hole == null)
                    continue;

                var holePoint = hole.CoordinateSequence.GetCoordinate(0);
                if (!shell.EnvelopeInternal.Contains(holePoint)) // Fast, but not precise.
                    continue;

                if (!PointLocation.IsInRing(holePoint, shell.CoordinateSequence))
                    continue;

                shellHoles.Add(hole);
                // holes.RemoveAt(i); // Avoid copying internal List's array over and over.
                holes[i] = null;
            }

            if (shellHoles.Count < 1)
                return NoHoles;

            return shellHoles.ToArray();
        }


        private static int CompareLinearRingAreas(LinearRing ring1, LinearRing ring2)
        {
            var area1 = Area.OfRing(ring1.CoordinateSequence);
            var area2 = Area.OfRing(ring2.CoordinateSequence);
            return area1.CompareTo(area2);
        }

        private static (List<LinearRing> Shells, List<LinearRing> Holes) GetShellsAndHoles(Core.ShpShapeBuilder shape, bool hasZ, bool hasM)
        {
            var shells = new List<LinearRing>(shape.PartCount);
            var holes = new List<LinearRing>();

            for (int partIndex = 0; partIndex < shape.PartCount; partIndex++)
            {
                var partCoordinates = shape.GetPartCoordinates(partIndex, hasZ, hasM, true);

                // Polygon must have at least 3 points
                if (partCoordinates.Count < 3)
                    continue;

                var ring = new LinearRing(partCoordinates, GeometryFactory.Default);

                // https://gis.stackexchange.com/a/147971/26684
                if (ring.IsCCW)
                {
                    holes.Add(ring);
                }
                else
                {
                    shells.Add(ring);
                }
            }

            // If there are only holes, without any shel (outer ring)
            if (shells.Count < 1 && holes.Count > 0)
            {
                foreach (var hole in holes)
                {
                    var reversedHole = new LinearRing(hole.CoordinateSequence.Reversed(), GeometryFactory.Default);
                    shells.Add(reversedHole);
                }
                holes.Clear();
            }

            return (shells, holes);
        }
    }
}
