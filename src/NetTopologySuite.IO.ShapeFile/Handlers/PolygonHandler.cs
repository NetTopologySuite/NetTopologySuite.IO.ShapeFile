using System;
using System.Collections.Generic;
using System.IO;
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
                return factory.CreatePolygon(null, null);

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

            // Get the resulting sequences
            var sequences = buffer.ToSequences(factory.CoordinateSequenceFactory);
            var shells = new List<LinearRing>();
            var holes = new List<LinearRing>();
            for (int i = 0; i < sequences.Length; i++)
            {
                //Skip garbage input data with 0 points
                if (sequences[i].Count < 1) continue;

                var tmp = EnsureClosedSequence(sequences[i], factory.CoordinateSequenceFactory);
                if (tmp == null) continue;
                var ring = factory.CreateLinearRing(tmp);
                if (ring.IsCCW)
                    holes.Add(ring);
                else
                    shells.Add(ring);
            }

            // Ensure the ring is encoded right
            if (shells.Count == 0 && holes.Count == 1)
            {
                shells.Add(factory.CreateLinearRing(holes[0].CoordinateSequence.Reversed()));
                holes.Clear();
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
                var testPt = testHole.GetCoordinateN(0);

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
                        holesForThisShell.Add(testHole);

                        //Suggested by Bruno.Labrecque
                        //A LinearRing should only be added to one outer shell
                        break;
                    }
                }
            }

            var polygons = new Polygon[shells.Count];
            for (int i = 0; i < shells.Count; i++)
                polygons[i] = (factory.CreatePolygon(shells[i], holesForShells[i].ToArray()));

            if (polygons.Length == 0)
                geom = factory.CreatePolygon();
            else if (polygons.Length == 1)
                geom = polygons[0];
            else
                geom = factory.CreateMultiPolygon(polygons);

            return geom;
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
            writer.Write((int) ShapeType);

            var box = multi.EnvelopeInternal;
            var bounds = GetEnvelopeExternal(factory.PrecisionModel,  box);
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
                var polygon = (Polygon) multi.Geometries[part];
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
                var poly = (Polygon) multi.Geometries[part];
                var shell = (LinearRing)poly.ExteriorRing;
                // shells in polygons are written clockwise
                var points = !shell.IsCCW
                    ? shell.CoordinateSequence
                    : shell.CoordinateSequence.Reversed();
                WriteCoords(points, writer, zList, mList);

                foreach(LinearRing hole in poly.InteriorRings)
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
