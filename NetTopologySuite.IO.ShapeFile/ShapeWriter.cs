using System;
using System.IO;
using GeoAPI.DataStructures;
using GeoAPI.Geometries;
//using NetTopologySuite.Geometries;
//using NetTopologySuite.Utilities;

namespace NetTopologySuite.IO
{
    /// <summary>
    /// Contains methods for writing a single <c>Geometry</c> in binary ESRI Shapefile format.
    /// </summary>
    public class ShapeWriter
    {
        /// <summary>
        /// Standard byte size for each complex point.
        /// Each complex point (LineString, Polygon, ...) contains
        ///     4 bytes for ShapeTypes and
        ///     32 bytes for bounding box.
        /// </summary>
        protected const int InitCount = 36;

        /// <summary>
        /// Creates a <coordinate>ShapeWriter</coordinate> that creates objects using a basic GeometryFactory.
        /// </summary>
        public ShapeWriter() { }

        /// <summary>
        /// Writes x- and y-ordinate of <paramref name="coordinate"/> to a stream using <paramref name="writer"/>
        /// </summary>
        /// <param name="coordinate">The coordinate to write</param>
        /// <param name="writer">The writer to use</param>
        [Obsolete("Use WriteCoordinates(ICoordinateSequence, BinaryWriter, Ordinates)")]
        public void Write(Coordinate coordinate, BinaryWriter writer)
        {
            writer.Write((double) coordinate.X);
            writer.Write((double) coordinate.Y);
        }

        /// <summary>
        /// Writes x- and y-ordinates of <paramref name="coordinates"/> to a stream using <paramref name="writer"/>
        /// </summary>
        /// <param name="coordinates">The array of <see cref="Coordinate"/>s to write</param>
        /// <param name="writer">The writer to use</param>
        [Obsolete("Use WriteCoordinates(ICoordinateSequence, BinaryWriter, Ordinates)")]
        public void Write(Coordinate[] coordinates, BinaryWriter writer)
        {
            foreach (var coordinate in coordinates)
            {
                writer.Write(coordinate.X);
                writer.Write(coordinate.Y);
            }
        }

        /// <summary>
        /// Write the <paramref name="ordinates"/> of the <paramref name="sequence"/> using the provided <paramref name="writer"/>
        /// </summary>
        /// <param name="sequence">The sequence</param>
        /// <param name="writer">The writer</param>
        /// <param name="ordinates">The ordinates, <see cref="Ordinates.X"/> and <see cref="Ordinates.Y"/> are written in any case.</param>
        protected void WriteCoordinates(ICoordinateSequence sequence, BinaryWriter writer, Ordinates ordinates)
        {
            for (int i = 0; i < sequence.Count; i++)
            {
                writer.Write(sequence.GetX(i));
                writer.Write(sequence.GetY(i));
            }

            if ((ordinates & Ordinates.Z) == Ordinates.Z)
            {
                WriteInterval(sequence, Ordinate.Z, writer);
                for (int i = 0; i < sequence.Count; i++)
                    writer.Write(GetOrdinate(sequence, Ordinate.Z, i));
            }

            if ((ordinates & Ordinates.M) == Ordinates.M)
            {
                WriteInterval(sequence, Ordinate.M, writer);
                for (int i = 0; i < sequence.Count; i++)
                    writer.Write(GetOrdinate(sequence, Ordinate.M, i));
            }
        }

        /// <summary>
        /// Evaluates the <see cref="Interval"/> of the <paramref name="ordinate"/>-values in
        /// <paramref name="sequence"/> and writes it using the provided <paramref name="writer"/>
        /// </summary>
        /// <param name="sequence">The sequence</param>
        /// <param name="ordinate">The ordinate</param>
        /// <param name="writer">The writer</param>
        protected void WriteInterval(ICoordinateSequence sequence, Ordinate ordinate, BinaryWriter writer)
        {
            double val = GetOrdinate(sequence, ordinate, 0);
            var interval = Interval.Create(val);
            for (int i = 1; i < sequence.Count; i++)
                interval = interval.ExpandedByValue(GetOrdinate(sequence, ordinate, i));

            writer.Write(interval.Min);
            writer.Write(interval.Max);
        }

        private static double GetOrdinate(ICoordinateSequence sequence, Ordinate ordinate, int index)
        {
            double val = sequence.GetOrdinate(index, ordinate);
            if (ordinate == Ordinate.M && double.IsNaN(val))
                val = ShapeFileConstants.NoDataValue;
            return val;
        }
        /// <summary>
        /// Writes <paramref name="point"/> to a stream using <paramref name="writer"/>
        /// </summary>
        /// <param name="point">The point to write</param>
        /// <param name="writer">The writer to use</param>
        public void Write(IPoint point, BinaryWriter writer)
        {
            writer.Write((int) ShapeGeometryType.Point);
            WriteCoordinates(point.CoordinateSequence, writer, Ordinates.XY);
        }

        /// <summary>
        /// Writes <paramref name="lineString"/> to a stream using <paramref name="writer"/>
        /// </summary>
        /// <param name="lineString">The <c>LineString</c> to write</param>
        /// <param name="writer">The writer to use</param>
        public void Write(ILineString lineString, BinaryWriter writer)
        {
            writer.Write((int) ShapeGeometryType.LineString);

            // Write BoundingBox
            WriteBoundingBox(lineString.EnvelopeInternal, writer);

            // Write NumParts and NumPoints
            writer.Write((int) 1);
            writer.Write((int) lineString.NumPoints);

            // Write IndexParts
            writer.Write((int) 0);

            // Write Coordinates
            WriteCoordinates(lineString.CoordinateSequence, writer, Ordinates.XY);
        }

        /// <summary>
        /// Writes <paramref name="polygon"/> to a stream using <paramref name="writer"/>
        /// </summary>
        /// <param name="polygon">The polygon to write</param>
        /// <param name="writer">The writer to use</param>
        public void Write(IPolygon polygon, BinaryWriter writer)
        {
            writer.Write((int) ShapeGeometryType.Polygon);

            // Write BoundingBox
            WriteBoundingBox(polygon.EnvelopeInternal, writer);

            // Write NumParts and NumPoints
            writer.Write((int) (polygon.NumInteriorRings + 1));
            writer.Write((int)  polygon.NumPoints);

            // Write IndexParts
            int count = 0;
            writer.Write((int) count);
            var seq = polygon.Factory.CoordinateSequenceFactory.Create(polygon.NumPoints,
                                                                       polygon.ExteriorRing.CoordinateSequence.Ordinates);

            // Gather coordinate information
            var ring = polygon.ExteriorRing.CoordinateSequence;
            Copy(ring, 0, seq, count, ring.Count);

            // If we have interior rings write the index parts and gather coordinate information
            if (polygon.NumInteriorRings > 0)
            {
                // Write exterior shell index
                count += ring.Count;
                writer.Write((int) count);

                // Gather coordinates and write interior shell index
                for (int i = 0; i < polygon.NumInteriorRings; i++)
                {
                    // Write internal holes index
                    ring = polygon.GetInteriorRingN(i).CoordinateSequence;
                    Copy(ring, 0, seq, count, ring.Count);
                    if (i < polygon.NumInteriorRings - 1)
                    {
                        count += ring.Count;
                        writer.Write((int) count);
                    }
                }
            }

            // Write Coordinates
            WriteCoordinates(seq, writer, Ordinates.XY);
        }

        /// <summary>
        /// Writes <paramref name="multiPoint"/> to a stream using <paramref name="writer"/>
        /// </summary>
        /// <param name="multiPoint">The multi point to write</param>
        /// <param name="writer">The writer to use</param>
        public void Write(IMultiPoint multiPoint, BinaryWriter writer)
        {
            writer.Write((int) ShapeGeometryType.MultiPoint);

            // Write BoundingBox
            WriteBoundingBox(multiPoint.EnvelopeInternal, writer);

            // Write NumPoints
            writer.Write((int) multiPoint.NumPoints);

            // Merge sequences of points into one
            var seq = multiPoint.Factory.CoordinateSequenceFactory.Create(
                multiPoint.NumGeometries, ((IPoint) multiPoint.GetGeometryN(0)).CoordinateSequence.Ordinates);
            for (int i = 0; i < seq.Count; i++)
            {
                var pt = (IPoint) multiPoint.GetGeometryN(i);
                seq.SetOrdinate(i, Ordinate.X, pt.CoordinateSequence.GetOrdinate(i, Ordinate.X));
                seq.SetOrdinate(i, Ordinate.Y, pt.CoordinateSequence.GetOrdinate(i, Ordinate.Y));
                if ((seq.Ordinates & Ordinates.Z) == Ordinates.Z)
                    seq.SetOrdinate(i, Ordinate.Z, pt.CoordinateSequence.GetOrdinate(i, Ordinate.Z));
                if ((seq.Ordinates & Ordinates.M) == Ordinates.M)
                    seq.SetOrdinate(i, Ordinate.M, pt.CoordinateSequence.GetOrdinate(i, Ordinate.M));
            }

            // Write Coordinates
            WriteCoordinates(seq, writer, seq.Ordinates);
        }

        /*
        /// <summary>
        ///
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        private static ICoordinateSequence BuildSequence(IGeometryCollection collection)
        {
            var seq = collection.Factory.CoordinateSequenceFactory.Create(collection.NumPoints,
                                                                          DetectOrdinates(collection));
            var count = 0;
            for (var i = 0; i < collection.Count; i++)
            {
                var tmp = collection.GetGeometryN(i);
                ICoordinateSequence tmpSeq = null;
                switch (tmp.OgcGeometryType)
                {

                    case OgcGeometryType.Point:
                        tmpSeq = ((IPoint) tmp).CoordinateSequence;
                        break;
                    case OgcGeometryType.LineString:
                        tmpSeq = ((ILineString)tmp).CoordinateSequence;
                        break;

                    case OgcGeometryType.Polygon:
                        var poly = (IPolygon) tmp;
                        tmpSeq = poly.ExteriorRing.CoordinateSequence;
                        if (poly.NumInteriorRings > 0)
                        {
                            CoordinateSequences.Copy(tmpSeq, 0, seq, count, tmpSeq.Count);
                            int j;
                            for (j = 0; j < poly.NumInteriorRings - 1; j++)
                            {
                                tmpSeq = poly.GetInteriorRingN(j).CoordinateSequence;
                                CoordinateSequences.Copy(tmpSeq, 0, seq, count, tmpSeq.Count);
                                count += tmpSeq.Count;
                            }
                            tmpSeq = poly.GetInteriorRingN(j).CoordinateSequence;
                        }
                        break;

                    default:
                        throw new ArgumentException("Invalid geometry type");
                }

                if (tmpSeq != null)
                {
                    CoordinateSequences.Copy(tmpSeq, 0, seq, count, tmpSeq.Count);
                    count += tmpSeq.Count;
                }
            }
            return seq;
        }
        */

        /*
        /// <summary>
        /// Function to determine the shape geometry type for the <paramref name="geometry"/>
        /// </summary>
        /// <param name="geometry">The geometry</param>
        /// <returns>The shape geometry type</returns>
        private static ShapeGeometryType DetectShapeType(IGeometry geometry)
        {
            var ordinates = DetectOrdinates(geometry);
            switch (geometry.OgcGeometryType)
            {
                case OgcGeometryType.Point:
                    if (ordinates == Ordinates.XYM)
                        return ShapeGeometryType.PointM;
                    if (ordinates == Ordinates.XYZ)
                        return ShapeGeometryType.PointZ;
                    if (ordinates == Ordinates.XYZM)
                        return ShapeGeometryType.PointZM;
                    return ShapeGeometryType.Point;

                case OgcGeometryType.MultiPoint:
                    if (ordinates == Ordinates.XYM)
                        return ShapeGeometryType.MultiPointM;
                    if (ordinates == Ordinates.XYZ)
                        return ShapeGeometryType.MultiPointZ;
                    if (ordinates == Ordinates.XYZM)
                        return ShapeGeometryType.MultiPointZM;
                    return ShapeGeometryType.MultiPoint;

                case OgcGeometryType.LineString:
                case OgcGeometryType.MultiLineString:
                    if (ordinates == Ordinates.XYM)
                        return ShapeGeometryType.LineStringM;
                    if (ordinates == Ordinates.XYZ)
                        return ShapeGeometryType.LineStringZ;
                    if (ordinates == Ordinates.XYZM)
                        return ShapeGeometryType.LineStringZM;
                    return ShapeGeometryType.LineString;

                case OgcGeometryType.Polygon:
                case OgcGeometryType.MultiPolygon:
                    if (ordinates == Ordinates.XYM)
                        return ShapeGeometryType.PolygonM;
                    if (ordinates == Ordinates.XYZ)
                        return ShapeGeometryType.PolygonZ;
                    if (ordinates == Ordinates.XYZM)
                        return ShapeGeometryType.PolygonZM;
                    return ShapeGeometryType.Polygon;

                default:
                    throw new ArgumentException("Invalid geometry type", "geometry");
            }
        }
        */

        /// <summary>
        /// Function to determine which ordinates are set in the <paramref name="geometry"/>.
        /// To do that, this function looks for the first geometry that has a <see cref="ICoordinateSequence"/> property.
        /// Assuming all other geometries have the same ordinates at hand.
        /// </summary>
        /// <param name="geometry">The geometry</param>
        /// <returns>The ordinates flag</returns>
        private static Ordinates DetectOrdinates(IGeometry geometry)
        {
            if (geometry is IPoint)
                return ((IPoint) geometry).CoordinateSequence.Ordinates;
            if (geometry is ILineString)
                return ((ILineString)geometry).CoordinateSequence.Ordinates;
            if (geometry is IPolygon)
                return ((IPolygon)geometry).ExteriorRing.CoordinateSequence.Ordinates;

            if (geometry.NumGeometries > 0)
                return DetectOrdinates(geometry.GetGeometryN(0));
            /*
            for (var i = 0; i < geometry.NumGeometries; i++)
            {
                return DetectOrdinates(geometry.GetGeometryN(i));
            }
            */
            throw new ArgumentException("Invalid or empty geometry");
            //Assert.ShouldNeverReachHere("No geometry found to detect ordinates");
            //return Ordinates.None;
        }

        /// <summary>
        /// Writes <paramref name="multiLineString"/> to a stream using <paramref name="writer"/>
        /// </summary>
        /// <param name="multiLineString">The <c>MultiLineString</c> to write</param>
        /// <param name="writer">The writer to use</param>
        public void Write(IMultiLineString multiLineString, BinaryWriter writer)
        {
            writer.Write((int) ShapeGeometryType.LineString);

            // Write BoundingBox
            WriteBoundingBox(multiLineString.EnvelopeInternal, writer);

            // Write NumParts and NumPoints
            writer.Write((int) multiLineString.NumGeometries);
            writer.Write((int) multiLineString.NumPoints);

            // Write IndexParts
            int count = 0;
            writer.Write((int) count);

            var seq = multiLineString.Factory.CoordinateSequenceFactory.Create(multiLineString.NumPoints,
                                                ((ILineString) multiLineString[0]).CoordinateSequence.Ordinates);
            // Write LineString's index
            for (int i = 0; i < multiLineString.NumGeometries; i++)
            {
                // Write internal holes index
                var ls = ((ILineString) multiLineString.GetGeometryN(i)).CoordinateSequence;
                Copy(ls, 0, seq, count, ls.Count);
                count += ls.Count;
                if (count == multiLineString.NumPoints)
                    break;
                writer.Write((int) count);
            }

            // Write Coordinates
            WriteCoordinates(seq, writer, seq.Ordinates);
        }

        /// <summary>
        /// Writes <paramref name="multiPolygon"/> to a stream using <paramref name="writer"/>
        /// </summary>
        /// <param name="multiPolygon">The multi polygon to write</param>
        /// <param name="writer">The writer to use</param>
        public void Write(IMultiPolygon multiPolygon, BinaryWriter writer)
        {
            writer.Write((int) ShapeGeometryType.Polygon);

            // Write BoundingBox
            WriteBoundingBox(multiPolygon.EnvelopeInternal, writer);

            // Write NumParts and NumPoints
            int numParts = multiPolygon.NumGeometries;              // Exterior rings count
            for (int i = 0; i < multiPolygon.NumGeometries; i++)    // Adding interior rings count
                numParts += ((IPolygon) multiPolygon.GetGeometryN(i)).NumInteriorRings;

            writer.Write((int) numParts);
            writer.Write((int) multiPolygon.NumPoints);

            // Create a sequence for all coordinates
            var seq = multiPolygon.Factory.CoordinateSequenceFactory.Create(
                multiPolygon.NumPoints, DetectOrdinates(multiPolygon));

            // Write IndexParts
            int count = 0;
            writer.Write((int) count);

            for (int i = 0; i < multiPolygon.NumGeometries; i++)
            {
                var polygon = (IPolygon) multiPolygon.GetGeometryN(i);
                var shell = polygon.ExteriorRing;
                Copy(shell.CoordinateSequence, 0, seq, count, shell.NumPoints);
                count += shell.NumPoints;
                if (count == multiPolygon.NumPoints)
                    break;
                writer.Write((int) count);
                for (int j = 0; j < polygon.NumInteriorRings; j++)
                {
                    var hole = (ILineString) polygon.GetInteriorRingN(j);
                    Copy(hole.CoordinateSequence, 0, seq, count, shell.NumPoints);
                    count += hole.NumPoints;
                    if (count == multiPolygon.NumPoints)
                        break;
                    writer.Write((int) count);
                }
            }

            // Write Coordinates
            WriteCoordinates(seq, writer, seq.Ordinates);
        }

        /// <summary>
        /// Writes the 2D <paramref name="boundingBox"/> using <paramref name="writer"/>
        /// </summary>
        /// <param name="boundingBox">The bounding box to write</param>
        /// <param name="writer">The writer</param>
        public void WriteBoundingBox(Envelope boundingBox, BinaryWriter writer)
        {
            writer.Write((double) boundingBox.MinX);
            writer.Write((double) boundingBox.MinY);
            writer.Write((double) boundingBox.MaxX);
            writer.Write((double) boundingBox.MaxY);
        }

        /// <summary>
        /// Sets correct length for Byte Stream.
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public byte[] GetBytes(IGeometry geometry)
        {
            return new byte[GetBytesLength(geometry)];
        }

        /// <summary>
        /// Return correct length for Byte Stream.
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public int GetBytesLength(IGeometry geometry)
        {
            if (geometry is IPoint)
                return SetByteStreamLength(geometry as IPoint);
            else if (geometry is ILineString)
                return SetByteStreamLength(geometry as ILineString);
            else if (geometry is IPolygon)
                return SetByteStreamLength(geometry as IPolygon);
            else if (geometry is IMultiPoint)
                return SetByteStreamLength(geometry as IMultiPoint);
            else if (geometry is IMultiLineString)
                return SetByteStreamLength(geometry as IMultiLineString);
            else if (geometry is IMultiPolygon)
                return SetByteStreamLength(geometry as IMultiPolygon);
            else if (geometry is IGeometryCollection)
                throw new NotSupportedException("GeometryCollection not supported!");
            else throw new ArgumentException("ShouldNeverReachHere!");
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="multiPolygon"></param>
        /// <returns></returns>
        protected int SetByteStreamLength(IMultiPolygon multiPolygon)
        {
            int numParts = multiPolygon.NumGeometries;               // Exterior rings count
            foreach (IPolygon polygon in multiPolygon.Geometries)    // Adding interior rings count
                numParts += polygon.NumInteriorRings;
            int numPoints = multiPolygon.NumPoints;
            return CalculateLength(numParts, numPoints);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="multiLineString"></param>
        /// <returns></returns>
        protected int SetByteStreamLength(IMultiLineString multiLineString)
        {
            int numParts = multiLineString.NumGeometries;
            int numPoints = multiLineString.NumPoints;
            return CalculateLength(numParts, numPoints);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="multiPoint"></param>
        /// <returns></returns>
        protected int SetByteStreamLength(IMultiPoint multiPoint)
        {
            int numPoints = multiPoint.NumPoints;
            return CalculateLength(numPoints);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        protected int SetByteStreamLength(IPolygon polygon)
        {
            int numParts = polygon.InteriorRings.Length + 1;
            int numPoints = polygon.NumPoints;
            return CalculateLength(numParts, numPoints);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="lineString"></param>
        /// <returns></returns>
        protected int SetByteStreamLength(ILineString lineString)
        {
            int numPoints = lineString.NumPoints;
            return CalculateLength(1, numPoints);   // ASSERT: IndexParts.Length == 1;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        protected int SetByteStreamLength(IPoint point)
        {
            return 20;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="numParts"></param>
        /// <param name="numPoints"></param>
        /// <returns></returns>
        private static int CalculateLength(int numParts, int numPoints)
        {
            int count = InitCount;
            count += 8;                         // NumParts and NumPoints
            count += 4 * numParts;
            count += 8 * 2 * numPoints;
            return count;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="numPoints"></param>
        /// <returns></returns>
        private static int CalculateLength(int numPoints)
        {
            int count = InitCount;
            count += 4;                         // NumPoints
            count += 8 * 2 * numPoints;
            return count;
        }

        ///<summary>
        /// Copies a section of a <see cref="ICoordinateSequence"/> to another <see cref="ICoordinateSequence"/>.
        /// The sequences may have different dimensions;
        /// in this case only the common dimensions are copied.
        ///</summary>
        /// <param name="src">The sequence to copy coordinates from</param>
        /// <param name="srcPos">The starting index of the coordinates to copy</param>
        /// <param name="dest">The sequence to which the coordinates should be copied to</param>
        /// <param name="destPos">The starting index of the coordinates in <see paramref="dest"/></param>
        /// <param name="length">The number of coordinates to copy</param>
        protected static void Copy(ICoordinateSequence src, int srcPos, ICoordinateSequence dest, int destPos, int length)
        {
            for (int i = 0; i < length; i++)
                CopyCoord(src, srcPos + i, dest, destPos + i);
        }

        ///<summary>
        /// Copies a coordinate of a <see cref="ICoordinateSequence"/> to another <see cref="ICoordinateSequence"/>.
        /// The sequences may contain different <see cref="Ordinates"/>; in this case only the common ordinates are copied.
        ///</summary>
        /// <param name="src">The sequence to copy coordinate from</param>
        /// <param name="srcPos">The index of the coordinate to copy</param>
        /// <param name="dest">The sequence to which the coordinate should be copied to</param>
        /// <param name="destPos">The index of the coordinate in <see paramref="dest"/></param>
        protected static void CopyCoord(ICoordinateSequence src, int srcPos, ICoordinateSequence dest, int destPos)
        {
            dest.SetOrdinate(destPos, Ordinate.X, src.GetOrdinate(srcPos, Ordinate.X));
            dest.SetOrdinate(destPos, Ordinate.Y, src.GetOrdinate(srcPos, Ordinate.Y));
            if ((src.Ordinates & dest.Ordinates & Ordinates.Z) == Ordinates.Z)
                dest.SetOrdinate(destPos, Ordinate.Z, src.GetOrdinate(srcPos, Ordinate.Z));
            if ((src.Ordinates & dest.Ordinates & Ordinates.M) == Ordinates.M)
                dest.SetOrdinate(destPos, Ordinate.M, src.GetOrdinate(srcPos, Ordinate.M));
        }

    }
}
