using System;
using System.Collections.Generic;
using System.IO;
using NetTopologySuite.Geometries;
using NetTopologySuite.Utilities;

namespace NetTopologySuite.IO.Handlers
{
    /// <summary>
    /// Converts a Shapefile point to a OGIS Polygon.
    /// </summary>
    public class MultiPointHandler : ShapeHandler
    {
        public MultiPointHandler() : base(ShapeGeometryType.MultiPoint) { }

        public MultiPointHandler(ShapeGeometryType type) : base(type) { }

        /// <summary>
        /// Reads a stream and converts the shapefile record to an equilivant geometry object.
        /// </summary>
        /// <param name="file">The stream to read.</param>
        /// <param name="totalRecordLength">Total length of the record we are about to read</param>
        /// <param name="factory">The geometry factory to use when making the object.</param>
        /// <returns>The Geometry object that represents the shape file record.</returns>
        public override Geometry Read(BigEndianBinaryReader file, int totalRecordLength, GeometryFactory factory)
        {
            int totalRead = 0;
            int shapeTypeNum = ReadInt32(file, totalRecordLength, ref totalRead);

            var type = (ShapeGeometryType) EnumUtility.Parse(typeof(ShapeGeometryType), shapeTypeNum.ToString());
            if (type == ShapeGeometryType.NullShape)
                return factory.CreateMultiPoint();

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

            // Read points
            int numPoints = ReadInt32(file, totalRecordLength, ref totalRead);
            var buffer = new CoordinateBuffer(numPoints, NoDataBorderValue, true);
            var points = new Point[numPoints];
            var pm = factory.PrecisionModel;

            for (int i = 0; i < numPoints; i++)
            {
                double x = pm.MakePrecise(ReadDouble(file, totalRecordLength, ref totalRead));
                double y = pm.MakePrecise(ReadDouble(file, totalRecordLength, ref totalRead));
                buffer.AddCoordinate(x, y);
                buffer.AddMarker();
            }

            // Trond Benum: We have now read all the points, let's read optional Z and M values
            GetZMValues(file, totalRecordLength, ref totalRead, buffer);

            var sequences = buffer.ToSequences(factory.CoordinateSequenceFactory);
            for (int i = 0; i < numPoints; i++)
                points[i] = factory.CreatePoint(sequences[i]);

            geom = factory.CreateMultiPoint(points);

            return geom;
        }

        /// <summary>
        /// Writes a Geometry to the given binary wirter.
        /// </summary>
        /// <param name="geometry">The geometry to write.</param>
        /// <param name="writer">The writer to use.</param>
        /// <param name="factory">The geometry factory to use.</param>
        public override void Write(Geometry geometry, BinaryWriter writer, GeometryFactory factory)
        {
            if (geometry == null)
                throw new ArgumentNullException("geometry");

            var mpoint = geometry as MultiPoint;
            if (mpoint == null)
            {
                string err = string.Format("Expected geometry that implements 'MultiPoint', but was '{0}'",
                    geometry.GetType().Name);
                throw new ArgumentException(err, "geometry");
            }

            // Slow and maybe not useful...
            // if (!geometry.IsValid)
            // Trace.WriteLine("Invalid multiPoint being written.");

            writer.Write((int)ShapeType);
            WriteEnvelope(writer, factory.PrecisionModel, geometry.EnvelopeInternal);

            int numPoints = mpoint.NumPoints;
            writer.Write(numPoints);

            bool hasZ = HasZValue();
            var zList = hasZ ? new List<double>() : null;

            bool hasM = HasMValue();
            var mList = hasM ? new List<double>() : null;

            // write the points
            for (int i = 0; i < numPoints; i++)
            {
                var point = (Point) mpoint.Geometries[i];

                writer.Write(point.X);
                writer.Write(point.Y);

                if (hasZ) zList.Add(point.Z);
                if (hasM) mList.Add(point.M);
            }

            WriteZM(writer, numPoints, zList, mList);
        }

        /// <summary>
        /// Gets the length of the shapefile record using the geometry passed in.
        /// </summary>
        /// <param name="geometry">The geometry to get the length for.</param>
        /// <returns>The length in bytes this geometry is going to use when written out as a shapefile record.</returns>
        public override int ComputeRequiredLengthInWords(Geometry geometry)
        {
            int numPoints = geometry.NumPoints;
            return ComputeRequiredLengthInWords(0, numPoints, HasMValue(), HasZValue());
            /*
            int pointFactor = 2 * sizeof(double) ; // xy (4*2)
            int initial = 20; // 20 => shapetype(2) + bbox (4*4) + numpoints

            if (HasZValue())
            {
                initial = initial + 16; // ZM 16 => bbox (4*4)
                pointFactor = pointFactor + 8; // ZM 8 => 4 * 2
            }
            else if (HasMValue())
            {
                initial = initial + 8; // 16 => bbox m (4*2)
                pointFactor = pointFactor + 4; // M 4 => 4 * 1
            }

            return (initial + geometry.NumPoints * pointFactor);
             */
        }
    }
}
