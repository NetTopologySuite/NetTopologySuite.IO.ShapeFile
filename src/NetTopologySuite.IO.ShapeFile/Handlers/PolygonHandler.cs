using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;

namespace NetTopologySuite.IO.Handlers
{

    /// <summary>
    /// Converts a Shapefile point to a OGIS Polygon.
    /// </summary>
    public class PolygonHandler : ShapeHandler
    {

        //Thanks to Bruno.Labrecque
        private static readonly ProbeLinearRing ProbeLinearRing = new ProbeLinearRing();

        public PolygonHandler() : base(ShapeGeometryType.Polygon)
        {
        }
        public PolygonHandler(ShapeGeometryType type)
            : base(type)
        {
        }

        /// <summary>
        /// Reads a stream and converts the shapefile record to an equilivent geometry object.
        /// </summary>
        /// <param name="file">The stream to read.</param>
        /// <param name="totalRecordLength">Total length of the record we are about to read</param>
        /// <param name="factory">The geometry factory to use when making the object.</param>
        /// <returns>The Geometry object that represents the shape file record.</returns>
        public override Geometry Read(BigEndianBinaryReader file, int totalRecordLength, GeometryFactory factory)
        {
            int totalRead = 0;
            var type = (ShapeGeometryType)ReadInt32(file, totalRecordLength, ref totalRead);
            if (type == ShapeGeometryType.NullShape)
                return factory.CreatePolygon();

            if (type != ShapeType)
                throw new ShapefileException(string.Format("Encountered a '{0}' instead of a  '{1}'", type, ShapeType));

            // Read and for now ignore bounds.
            int bblength = GetBoundingBoxLength();
            boundingBox = new double[bblength];
            for (; boundingBoxIndex < 4; boundingBoxIndex++)
                boundingBox[boundingBoxIndex] = ReadDouble(file, totalRecordLength, ref totalRead);

            int numParts = ReadInt32(file, totalRecordLength, ref totalRead);
            int numPoints = ReadInt32(file, totalRecordLength, ref totalRead);
            int[] partOffsets = new int[numParts];
            for (int i = 0; i < numParts; i++)
                partOffsets[i] = ReadInt32(file, totalRecordLength, ref totalRead);

            var skippedList = new HashSet<int>();

            //var allPoints = new List<Coordinate>();
            var buffer = new CoordinateBuffer(numPoints, NoDataBorderValue, true);
            var pm = factory.PrecisionModel;
            for (int part = 0; part < numParts; part++)
            {
                int start = partOffsets[part];
                int finish = (part == numParts - 1)
                    ? numPoints
                    : partOffsets[part + 1];

                int length = finish - start;
                for (int i = 0; i < length; i++)
                {
                    double x = pm.MakePrecise(ReadDouble(file, totalRecordLength, ref totalRead));
                    double y = pm.MakePrecise(ReadDouble(file, totalRecordLength, ref totalRead));

                    // Thanks to Abhay Menon!
                    if (!(Coordinate.NullOrdinate.Equals(x) || Coordinate.NullOrdinate.Equals(y)))
                        buffer.AddCoordinate(x, y);
                    else
                        skippedList.Add(start + i);
                }
                //Add a marker that we have finished one part of the geometry
                buffer.AddMarker();
            }

            // Trond Benum: We have now read all the parts, let's read optional Z and M values
            // and populate Z in the coordinate before we start manipulating the segments
            // We have to track corresponding optional M values and set them up in the
            // Geometries via CoordinateSequence further down.
            GetZMValues(file, totalRecordLength, ref totalRead, buffer, skippedList);

            Polygon[] polys;
            switch (Shapefile.PolygonBuilder)
            {
                case PolygonBuilder.Extended:
                    polys = InternalBuildPolygonsExperimental(factory, buffer);
                    break;
                case PolygonBuilder.Sequential:
                    polys = InternalBuildPolygonsEx2(factory, buffer);
                    break;
                case PolygonBuilder.UsePolygonizer:
                    polys = InternalBuildPolygonsEx3(factory, buffer);
                    break;
                default:
                    polys = InternalBuildPolygons(factory, buffer);
                    break;
            }

            if (polys.Length == 0)
                geom = factory.CreatePolygon();
            else if (polys.Length == 1)
                geom = polys[0];
            else
                geom = factory.CreateMultiPolygon(polys);
            return geom;
        }

        private static Polygon[] InternalBuildPolygons(GeometryFactory factory, CoordinateBuffer buffer)
        {
            // Get the resulting sequences
            var sequences = buffer.ToSequences(factory.CoordinateSequenceFactory);
            var shells = new List<Polygon>();
            var holes = new List<Polygon>();
            for (int i = 0; i < sequences.Length; i++)
            {
                //Skip garbage input data with 0 points
                if (sequences[i].Count < 1) continue;

                var tmp = EnsureClosedSequence(sequences[i], factory.CoordinateSequenceFactory);
                if (tmp == null) continue;
                var ring = factory.CreateLinearRing(tmp);
                if (ring.IsCCW)
                    holes.Add(factory.CreatePolygon(ring));
                else
                    shells.Add(factory.CreatePolygon(ring));
            }

            // Ensure the ring is encoded right
            if (shells.Count == 0 && holes.Count == 1)
            {
                shells.Add((Polygon)((Geometry)holes[0]).Reverse());
                holes.Clear();

                // We have one polygon, we don't have to do anything else
                return shells.ToArray();
            }

            // Now we have lists of all shells and all holes
            var holesForShells = new List<List<LinearRing>>(shells.Count);
            for (int i = 0; i < shells.Count; i++)
                holesForShells.Add(new List<LinearRing>());

            //Thanks to Bruno.Labrecque
            //Sort shells by area, rings should only be added to the smallest shell, that contains the ring
            shells.Sort(ProbeLinearRing);

            // Find holes
            foreach (var testHole in holes)
            {
                var testEnv = testHole.EnvelopeInternal;
                var testPt = testHole.ExteriorRing.GetCoordinateN(0);

                //We have the shells sorted
                for (int j = 0; j < shells.Count; j++)
                {
                    var tryShell = shells[j];
                    var tryEnv = tryShell.EnvelopeInternal;
                    bool isContained = tryEnv.Contains(testEnv) && PointLocation.IsInRing(testPt, tryShell.Coordinates);

                    // Check if this new containing ring is smaller than the current minimum ring
                    if (isContained)
                    {
                        // Suggested by Brian Macomber and added 3/28/2006:
                        // holes were being found but never added to the holesForShells array
                        // so when converted to geometry by the factory, the inner rings were never created.
                        var holesForThisShell = holesForShells[j];
                        holesForThisShell.Add((LinearRing)testHole.ExteriorRing);

                        //Suggested by Bruno.Labrecque
                        //A LinearRing should only be added to one outer shell
                        break;
                    }
                }
            }

            var polygons = new Polygon[shells.Count];
            for (int i = 0; i < shells.Count; i++)
                polygons[i] = factory.CreatePolygon((LinearRing)shells[i].ExteriorRing, holesForShells[i].ToArray());
            return polygons;
        }

        private static Polygon[] InternalBuildPolygonsExperimental(GeometryFactory factory, CoordinateBuffer buffer)
        {
            // Get the resulting sequences
            var sequences = buffer.ToSequences(factory.CoordinateSequenceFactory);
            // Read all rings
            var rings = new List<Polygon>();
            for (int i = 0; i < sequences.Length; i++)
            {
                // Skip garbage input data with 0 points
                if (sequences[i].Count < 1) continue;

                var tmp = EnsureClosedSequence(sequences[i], factory.CoordinateSequenceFactory);
                if (tmp == null) continue;
                var ring = factory.CreateLinearRing(tmp);
                rings.Add(factory.CreatePolygon(ring));
            }

            // Utility function to test if a ring is a potential hole for a shell
            bool IsHoleContainedInShell(LinearRing shell, LinearRing hole) =>
                shell.EnvelopeInternal.Contains(hole.EnvelopeInternal)
                && PointLocation.IsInRing(hole.GetCoordinateN(0), shell.Coordinates);

            // Sort rings by area, from bigger to smaller
            rings = rings.OrderByDescending(r => r.Area).ToList();

            // Considering all rings as a potential shell, search the valid holes for any shell
            // NOTE: rings order explained: https://gis.stackexchange.com/a/147971/26684
            var data = new Stack<(LinearRing shell, List<LinearRing> holes)>(); // LIFO
            for (int i = 0; i < rings.Count; i++)
            {
                var ring = (LinearRing)rings[i].ExteriorRing;
                if (i == 0)
                {
                    // First ring is "by design" a shell
                    data.Push((ring, new List<LinearRing>()));
                    continue;
                }

                bool isHoleForShell = false;
                foreach (var (tryShell, tryHoles) in data)
                {
                    // Check if the ring is inside any shell: if true,
                    // it can be considered a potential hole for the shell
                    if (IsHoleContainedInShell(tryShell, ring))
                    {
                        // Check if the ring is inside any hole of the shell:
                        // if true, this means that is actually a shell of a distinct
                        // geometry,and NOT a valid hole for the shell; a hole
                        // inside another hole is not allowed
                        if (!tryHoles.Any(tryHole => IsHoleContainedInShell(tryHole, ring)))
                        {
                            tryHoles.Add(ring);
                            isHoleForShell = true;
                            break;
                        }
                    }
                }
                if (!isHoleForShell)
                {
                    data.Push((ring, new List<LinearRing>()));
                }
            }

            return data
                .Select(t => factory.CreatePolygon(t.shell, t.holes.ToArray()))
                .ToArray();
        }

        private static Polygon[] InternalBuildPolygonsEx2(GeometryFactory factory, CoordinateBuffer buffer)
        {
            // Get the resulting sequences
            var sequences = buffer.ToSequences(factory.CoordinateSequenceFactory);

            // We do not sort the rings but assume that polygons are serialized in the following
            // order: Shell[, Holes]. This leads to the conclusion that the first ring that is not
            // contained by the current polygon, is the start of a new polygon.
            LinearRing shell = null;
            var holes = new List<LinearRing>(sequences.Length - 1);

            // Utility function to test if a ring is a potential hole for a shell
            bool IsRingContainedByShell(LinearRing testShell, LinearRing testRing) =>
                shell.EnvelopeInternal.Contains(testRing.EnvelopeInternal)
                && PointLocation.IsInRing(testRing.GetCoordinateN(0), shell.CoordinateSequence);


            var res = new List<Polygon>(sequences.Length);
            for (int i = 0; i < sequences.Length; i++)
            {
                // Skip garbage input data with 0 points
                if (sequences[i].Count < 1) continue;

                var tmp = EnsureClosedSequence(sequences[i], factory.CoordinateSequenceFactory);
                var testRing = factory.CreateLinearRing(tmp);
                if (shell == null)
                {
                    shell = testRing;
                }
                else
                {
                    // Flag indicating if we this ring starts a new polygon
                    bool newPolygon = false;

                    // If the ring is not contained by the shell, we have a new polygon
                    if (!IsRingContainedByShell(shell, testRing))
                    {
                        newPolygon = true;
                    }
                    else
                    {
                        // If any hole contains this ring, we have a new polygon
                        foreach (var hole in holes)
                        {
                            if (IsRingContainedByShell((LinearRing)hole, testRing))
                            {
                                newPolygon = true;
                                break;
                            }
                        }

                        // Otherwise add this ring to the holes list
                        holes.Add(testRing);
                    }

                    // If we have to start a new polygon we do it now
                    if (newPolygon)
                    {
                        res.Add(factory.CreatePolygon(shell, holes.ToArray()));
                        shell = testRing;
                        holes.Clear();
                    }
                }
            }

            if (shell != null)
                res.Add(factory.CreatePolygon(shell, holes.ToArray()));

            return res.ToArray();

            //// We do not sort the rings but assume that polygons are serialized in the following
            //// order: Shell[, Holes]. This leads to the conclusion that the first ring that is not
            //// contained by the current polygon, is the start of a new polygon.
            //var res = new List<Polygon>();
            //var currentPolygon = factory.CreatePolygon(rings[0]);
            //res.Add(currentPolygon);

            //var holes = new List<LinearRing>();
            //for (int i = 1; i < rings.Count; i++)
            //{
            //    if (currentPolygon.Contains(rings[i]))
            //    {
            //        holes.Add(rings[i]);
            //        currentPolygon = factory.CreatePolygon((LinearRing)currentPolygon.ExteriorRing, holes.ToArray());
            //    }
            //    else
            //    {
            //        holes.Clear();
            //        currentPolygon = factory.CreatePolygon(rings[i]);
            //    }
            //}

            //return res.ToArray();
        }

        private static Polygon[] InternalBuildPolygonsEx3(GeometryFactory factory, CoordinateBuffer buffer)
        {
            // Get the resulting sequences
            var sequences = buffer.ToSequences(factory.CoordinateSequenceFactory);

            // Add rings to polygonizer
            var polygonizer = new Operation.Polygonize.Polygonizer(true);
            polygonizer.IsCheckingRingsValid = false;
            for (int i = 0; i < sequences.Length; i++)
            {
                // Skip garbage input data with 0 points
                if (sequences[i].Count < 1) continue;

                var tmp = EnsureClosedSequence(sequences[i], factory.CoordinateSequenceFactory);
                if (tmp == null) continue;
                polygonizer.Add(factory.CreateLinearRing(tmp));
            }

            var polygonal = polygonizer.GetGeometry();
            if (polygonal is MultiPolygon mp)
                return mp.Geometries.Cast<Polygon>().ToArray();

            return new [] {(Polygon)polygonal}; 
        }

        /// <summary>
        /// Writes a Geometry to the given binary wirter.
        /// </summary>
        /// <param name="geometry">The geometry to write.</param>
        /// <param name="writer">The file stream to write to.</param>
        /// <param name="factory">The geometry factory to use.</param>
        public override void Write(Geometry geometry, BinaryWriter writer, GeometryFactory factory)
        {
            if (geometry == null)
                throw new ArgumentNullException("geometry");

            // This check seems to be not useful and slow the operations...
            // if (!geometry.IsValid)
            // Trace.WriteLine("Invalid polygon being written.");

            var multi = geometry as MultiPolygon;
            if (multi == null)
            {
                var poly = geometry as Polygon;
                if (poly == null)
                {
                    string err = string.Format("Expected geometry that implements 'MultiPolygon' or 'Polygon', but was '{0}'",
                        geometry.GetType().Name);
                    throw new ArgumentException(err, "geometry");
                }

                var arr = new[] { poly };
                multi = factory.CreateMultiPolygon(arr);
            }

            // Write the shape type
            writer.Write((int)ShapeType);

            var box = multi.EnvelopeInternal;
            var bounds = GetEnvelopeExternal(factory.PrecisionModel, box);
            writer.Write(bounds.MinX);
            writer.Write(bounds.MinY);
            writer.Write(bounds.MaxX);
            writer.Write(bounds.MaxY);

            int numParts = GetNumParts(multi);
            int numPoints = multi.NumPoints;
            writer.Write(numParts);
            writer.Write(numPoints);

            // write the offsets to the points
            int offset = 0;
            for (int part = 0; part < multi.NumGeometries; part++)
            {
                // offset to the shell points
                var polygon = (Polygon)multi.Geometries[part];
                writer.Write(offset);
                offset = offset + polygon.ExteriorRing.NumPoints;

                // offses to the holes
                foreach (LinearRing ring in polygon.InteriorRings)
                {
                    writer.Write(offset);
                    offset = offset + ring.NumPoints;
                }
            }

            var zList = HasZValue() ? new List<double>() : null;
            var mList = (HasMValue() || HasZValue()) ? new List<double>() : null;

            // write the points
            for (int part = 0; part < multi.NumGeometries; part++)
            {
                var poly = (Polygon)multi.Geometries[part];
                var shell = (LinearRing)poly.ExteriorRing;
                // shells in polygons are written clockwise
                var points = !shell.IsCCW
                    ? shell.CoordinateSequence
                    : shell.CoordinateSequence.Reversed();
                WriteCoords(points, writer, zList, mList);

                foreach (LinearRing hole in poly.InteriorRings)
                {
                    // holes in polygons are written counter-clockwise
                    points = hole.IsCCW
                        ? hole.CoordinateSequence
                        : hole.CoordinateSequence.Reversed();

                    WriteCoords(points, writer, zList, mList);
                }
            }

            //Write the z-m-values
            WriteZM(writer, multi.NumPoints, zList, mList);
        }

        /// <summary>
        /// Gets the length of the shapefile record using the geometry passed in.
        /// </summary>
        /// <param name="geometry">The geometry to get the length for.</param>
        /// <returns>The length in bytes this geometry is going to use when written out as a shapefile record.</returns>
        public override int ComputeRequiredLengthInWords(Geometry geometry)
        {
            int numParts = GetNumParts(geometry);
            int numPoints = geometry.NumPoints;

            return ComputeRequiredLengthInWords(numParts, numPoints, HasMValue(), HasZValue());
        }

        /// <summary>
        /// Method to compute the number of parts to write
        /// </summary>
        /// <param name="geometry">The geometry to write</param>
        /// <returns>The number of geometry parts</returns>
        private static int GetNumParts(Geometry geometry)
        {
            if (geometry == null)
                throw new ArgumentNullException("geometry");

            var mpoly = geometry as MultiPolygon;
            if (mpoly != null)
            {
                int numParts = 0;
                foreach (var geom in mpoly.Geometries)
                {
                    var part = (Polygon)geom;
                    numParts = numParts + part.InteriorRings.Length + 1;
                }
                return numParts;
            }

            var poly = geometry as Polygon;
            if (poly != null)
            {
                return poly.InteriorRings.Length + 1;
            }

            string err = string.Format("Expected geometry that implements 'MultiPolygon' or 'Polygon', but was '{0}'",
                geometry.GetType().Name);
            throw new ArgumentException(err, "geometry");
        }

        /// <summary>
        /// Function to return a coordinate sequence that is ensured to be closed.
        /// </summary>
        /// <param name="sequence">The base sequence</param>
        /// <param name="factory">The factory to use in case we need to create a new sequence</param>
        /// <returns>A closed coordinate sequence</returns>
        private static CoordinateSequence EnsureClosedSequence(CoordinateSequence sequence,
                                                                CoordinateSequenceFactory factory)
        {
            //This sequence won't serve a valid linear ring
            if (sequence.Count < 3)
                return null;

            //The sequence is closed
            var start = sequence.GetCoordinate(0);
            int lastIndex = sequence.Count - 1;
            var end = sequence.GetCoordinate(lastIndex);
            if (start.Equals2D(end))
                return sequence;

            // The sequence is not closed
            // 1. Test for a little offset, in that case simply correct x- and y- ordinate values
            const double eps = 1E-7;
            if (start.Distance(end) < eps)
            {
                sequence.SetX(lastIndex, start.X);
                sequence.SetY(lastIndex, start.Y);
                return sequence;
            }

            // 2. Close the sequence by adding a new point, this is heavier
            var newSequence = factory.Create(sequence.Count + 1, sequence.Dimension, sequence.Measures);
            int maxDim = sequence.Dimension;
            for (int i = 0; i < sequence.Count; i++)
            {
                for (int dim = 0; dim < maxDim; dim++)
                {
                    newSequence.SetOrdinate(i, dim, sequence.GetOrdinate(i, dim));
                }
            }

            for (int dim = 0; dim < maxDim; dim++)
            {
                newSequence.SetOrdinate(sequence.Count, dim, sequence.GetOrdinate(0, dim));
            }

            return newSequence;
        }

        /*
        /// <summary>
        /// Test if a point is in a list of coordinates.
        /// </summary>
        /// <param name="testPoint">TestPoint the point to test for.</param>
        /// <param name="pointList">PointList the list of points to look through.</param>
        /// <returns>true if testPoint is a point in the pointList list.</returns>
        private static bool PointInSequence(Coordinate testPoint, CoordinateSequence pointList)
        {
            for (var i = 0; i < pointList.Count; i++)
            {
                var p = pointList.GetCoordinate(i);
                if (p.Equals2D(testPoint))
                        return true;
            }
            return false;
        }
         */
    }
}
