using System;
using System.Collections.Generic;
using System.IO;
using NetTopologySuite.Geometries;

namespace NetTopologySuite.IO.Handlers
{
    /// <summary>
    /// Converts a Shapefile multi-line to a OGIS LineString/MultiLineString.
    /// </summary>
    public class MultiLineHandler : ShapeHandler
    {
        public MultiLineHandler() : base(ShapeGeometryType.LineString) { }

        public MultiLineHandler(ShapeGeometryType type) : base(type) { }

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
                return factory.CreateMultiLineString(null);

            if (type != ShapeType)
                throw new ShapefileException(string.Format("Encountered a '{0}' instead of a  '{1}'", type, ShapeType));

            // Read and for now ignore bounds.
            int bblength = GetBoundingBoxLength();
            boundingBox = new double[bblength];
            for (; boundingBoxIndex < 4; boundingBoxIndex++)
            {
                double d = ReadDouble(file, totalRecordLength, ref totalRead);
                boundingBox[boundingBoxIndex] = d;
            }

            int numParts = ReadInt32(file, totalRecordLength, ref totalRead);
            int numPoints = ReadInt32(file, totalRecordLength, ref totalRead);
            int[] partOffsets = new int[numParts];
            for (int i = 0; i < numParts; i++)
                partOffsets[i] = ReadInt32(file, totalRecordLength, ref totalRead);

            var lines = new List<LineString>(numParts);
            var buffer = new CoordinateBuffer(numPoints, NoDataBorderValue, true);
            var pm = factory.PrecisionModel;

            for (int part = 0; part < numParts; part++)
            {
                int start = partOffsets[part];
                int finish = part == numParts - 1
                                 ? numPoints
                                 : partOffsets[part + 1];
                int length = finish - start;

                for (int i = 0; i < length; i++)
                {
                    double x = pm.MakePrecise(ReadDouble(file, totalRecordLength, ref totalRead));
                    double y = pm.MakePrecise(ReadDouble(file, totalRecordLength, ref totalRead));
                    buffer.AddCoordinate(x, y);
                }
                buffer.AddMarker();
            }

            // Trond Benum: We have now read all the parts, let's read optional Z and M values
            // and populate Z in the coordinate before we start manipulating the segments
            // We have to track corresponding optional M values and set them up in the
            // Geometries via CoordinateSequence further down.
            GetZMValues(file, totalRecordLength, ref totalRead, buffer);

            var sequences = new List<CoordinateSequence>(buffer.ToSequences(factory.CoordinateSequenceFactory));

            for (int s = 0; s < sequences.Count; s++)
            {
                var points = sequences[s];

                //Skip garbage input data with 0 points
                if (points.Count < 1) continue;

                bool createLineString = true;
                if (points.Count == 1)
                {
                    switch (GeometryInstantiationErrorHandling)
                    {
                        case GeometryInstantiationErrorHandlingOption.ThrowException:
                            break;
                        case GeometryInstantiationErrorHandlingOption.Empty:
                            points = factory.CoordinateSequenceFactory.Create(0, points.Ordinates);
                            break;
                        case GeometryInstantiationErrorHandlingOption.TryFix:
                            points = AddCoordinateToSequence(points, factory.CoordinateSequenceFactory,
                                points.GetX(0), points.GetY(0),
                                points.GetZ(0), points.GetM(0));
                            break;
                        case GeometryInstantiationErrorHandlingOption.Null:
                            createLineString = false;
                            break;
                    }
                }

                if (createLineString)
                {
                    // Grabs m values if we have them
                    var line = factory.CreateLineString(points);
                    lines.Add(line);
                }
            }

            geom = (lines.Count != 1)
                ? (Geometry)factory.CreateMultiLineString(lines.ToArray())
                : lines[0];
            return geom;
        }

        /// <summary>
        /// Writes to the given stream the equilivent shape file record given a Geometry object.
        /// </summary>
        /// <param name="geometry">The geometry object to write.</param>
        /// <param name="writer">The stream to write to.</param>
        /// <param name="factory">The geometry factory to use.</param>
        public override void Write(Geometry geometry, BinaryWriter writer, GeometryFactory factory)
        {
            if (geometry == null)
                throw new ArgumentNullException("geometry");

            var multi = geometry as MultiLineString;
            if (multi == null)
            {
                var ls = geometry as LineString;
                if (ls == null)
                {
                    string err = string.Format("Expected geometry that implements 'MultiLineString' or 'LineString', but was '{0}'",
                        geometry.GetType().Name);
                    throw new ArgumentException(err, "geometry");
                }

                var arr = new[] { ls };
                multi = factory.CreateMultiLineString(arr);
            }

            writer.Write((int)ShapeType);

            var box = multi.EnvelopeInternal;
            writer.Write(box.MinX);
            writer.Write(box.MinY);
            writer.Write(box.MaxX);
            writer.Write(box.MaxY);

            int numParts = multi.NumGeometries;
            int numPoints = multi.NumPoints;

            writer.Write(numParts);
            writer.Write(numPoints);

            // Write the offsets
            int offset = 0;
            for (int i = 0; i < numParts; i++)
            {
                var g = multi.GetGeometryN(i);
                writer.Write(offset);
                offset = offset + g.NumPoints;
            }

            var zList = HasZValue() ? new List<double>() : null;
            var mList = HasMValue() ? new List<double>() : null;

            for (int part = 0; part < numParts; part++)
            {
                var geometryN = (LineString)multi.GetGeometryN(part);
                var points = geometryN.CoordinateSequence;
                WriteCoords(points, writer, zList, mList);
            }

            WriteZM(writer, numPoints, zList, mList);
        }

        /// <summary>
        /// Gets the length in bytes the Geometry will need when written as a shape file record.
        /// </summary>
        /// <param name="geometry">The Geometry object to use.</param>
        /// <returns>The length in bytes the Geometry will use when represented as a shape file record.</returns>
        public override int ComputeRequiredLengthInWords(Geometry geometry)
        {
            int numParts = GetNumParts(geometry);
            int numPoints = geometry.NumPoints;

            return ComputeRequiredLengthInWords(numParts, numPoints, HasMValue(), HasZValue());
        }

        private static int GetNumParts(Geometry geometry)
        {
            if (geometry == null)
                throw new ArgumentNullException("geometry");

            var mls = geometry as MultiLineString;
            if (mls != null)
                return mls.Geometries.Length;

            var ls = geometry as LineString;
            if (ls != null)
                return 1;

            string err = string.Format("Expected geometry that implements 'MultiLineString' or 'LineString', but was '{0}'",
                geometry.GetType().Name);
            throw new ArgumentException(err, "geometry");
        }
    }
}
