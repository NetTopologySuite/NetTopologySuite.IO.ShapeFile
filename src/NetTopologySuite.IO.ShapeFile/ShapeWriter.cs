using System;
using System.IO;

using NetTopologySuite.DataStructures;
using NetTopologySuite.Geometries;

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
        /// Write the <paramref name="ordinates"/> of the <paramref name="sequence"/> using the provided <paramref name="writer"/>
        /// </summary>
        /// <param name="sequence">The sequence</param>
        /// <param name="writer">The writer</param>
        /// <param name="ordinates">The ordinates, <see cref="Ordinates.X"/> and <see cref="Ordinates.Y"/> are written in any case.</param>
        protected void WriteCoordinates(CoordinateSequence sequence, BinaryWriter writer, Ordinates ordinates)
        {
            for (int i = 0; i < sequence.Count; i++)
            {
                writer.Write(sequence.GetX(i));
                writer.Write(sequence.GetY(i));
            }

            if ((ordinates & Ordinates.Z) == Ordinates.Z)
            {
                WriteInterval(sequence, Ordinate.Z, writer);
                for (int i = 0, cnt = sequence.Count; i < cnt; i++)
                {
                    writer.Write(sequence.GetZ(i));
                }
            }

            if ((ordinates & Ordinates.M) == Ordinates.M)
            {
                WriteInterval(sequence, Ordinate.M, writer);
                for (int i = 0, cnt = sequence.Count; i < cnt; i++)
                {
                    double val = sequence.GetM(i);
                    if (double.IsNaN(val))
                    {
                        val = ShapeFileConstants.NoDataValue;
                    }

                    writer.Write(val);
                }
            }
        }

        /// <summary>
        /// Evaluates the <see cref="Interval"/> of the <paramref name="ordinate"/>-values in
        /// <paramref name="sequence"/> and writes it using the provided <paramref name="writer"/>
        /// </summary>
        /// <param name="sequence">The sequence</param>
        /// <param name="ordinate">The ordinate</param>
        /// <param name="writer">The writer</param>
        protected void WriteInterval(CoordinateSequence sequence, Ordinate ordinate, BinaryWriter writer)
        {
            Interval interval;
            if (!sequence.TryGetOrdinateIndex(ordinate, out int ordinateIndex))
            {
                interval = ordinate == Ordinate.M
                    ? Interval.Create(ShapeFileConstants.NoDataValue)
                    : Interval.Create(double.NaN);
            }
            else if (ordinate == Ordinate.M)
            {
                double val = sequence.GetOrdinate(0, ordinateIndex);
                if (double.IsNaN(val))
                {
                    val = ShapeFileConstants.NoDataValue;
                }

                interval = Interval.Create(val);
                for (int i = 1, cnt = sequence.Count; i < cnt; i++)
                {
                    val = sequence.GetOrdinate(i, ordinateIndex);
                    if (double.IsNaN(val))
                    {
                        val = ShapeFileConstants.NoDataValue;
                    }

                    interval = interval.ExpandedByValue(val);
                }
            }
            else
            {
                double val = sequence.GetOrdinate(0, ordinateIndex);
                interval = Interval.Create(val);
                for (int i = 1, cnt = sequence.Count; i < cnt; i++)
                {
                    interval = interval.ExpandedByValue(sequence.GetOrdinate(i, ordinateIndex));
                }
            }

            writer.Write(interval.Min);
            writer.Write(interval.Max);
        }

        /// <summary>
        /// Writes <paramref name="point"/> to a stream using <paramref name="writer"/>
        /// </summary>
        /// <param name="point">The point to write</param>
        /// <param name="writer">The writer to use</param>
        public void Write(Point point, BinaryWriter writer)
        {
            writer.Write((int) ShapeGeometryType.Point);
            WriteCoordinates(point.CoordinateSequence, writer, Ordinates.XY);
        }

        /// <summary>
        /// Writes <paramref name="lineString"/> to a stream using <paramref name="writer"/>
        /// </summary>
        /// <param name="lineString">The <c>LineString</c> to write</param>
        /// <param name="writer">The writer to use</param>
        public void Write(LineString lineString, BinaryWriter writer)
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
        public void Write(Polygon polygon, BinaryWriter writer)
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
        public void Write(MultiPoint multiPoint, BinaryWriter writer)
        {
            writer.Write((int) ShapeGeometryType.MultiPoint);

            // Write BoundingBox
            WriteBoundingBox(multiPoint.EnvelopeInternal, writer);

            // Write NumPoints
            writer.Write((int) multiPoint.NumPoints);

            // Merge sequences of points into one
            var seq = multiPoint.Factory.CoordinateSequenceFactory.Create(
                multiPoint.NumGeometries, ((Point) multiPoint.GetGeometryN(0)).CoordinateSequence.Ordinates);
            for (int i = 0; i < seq.Count; i++)
            {
                var pt = (Point) multiPoint.GetGeometryN(i);
                seq.SetX(i, pt.CoordinateSequence.GetX(i));
                seq.SetY(i, pt.CoordinateSequence.GetY(i));
                if (seq.HasZ)
                {
                    seq.SetZ(i, pt.CoordinateSequence.GetZ(i));
                }

                if (seq.HasM)
                {
                    seq.SetM(i, pt.CoordinateSequence.GetM(i));
                }
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
        private static CoordinateSequence BuildSequence(GeometryCollection collection)
        {
            var seq = collection.Factory.CoordinateSequenceFactory.Create(collection.NumPoints,
                                                                          DetectOrdinates(collection));
            var count = 0;
            for (var i = 0; i < collection.Count; i++)
            {
                var tmp = collection.GetGeometryN(i);
                CoordinateSequence tmpSeq = null;
                switch (tmp.OgcGeometryType)
                {

                    case OgcGeometryType.Point:
                        tmpSeq = ((Point) tmp).CoordinateSequence;
                        break;
                    case OgcGeometryType.LineString:
                        tmpSeq = ((LineString)tmp).CoordinateSequence;
                        break;

                    case OgcGeometryType.Polygon:
                        var poly = (Polygon) tmp;
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
        private static ShapeGeometryType DetectShapeType(Geometry geometry)
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
        /// To do that, this function looks for the first geometry that has a <see cref="CoordinateSequence"/> property.
        /// Assuming all other geometries have the same ordinates at hand.
        /// </summary>
        /// <param name="geometry">The geometry</param>
        /// <returns>The ordinates flag</returns>
        private static Ordinates DetectOrdinates(Geometry geometry)
        {
            if (geometry is Point)
                return ((Point) geometry).CoordinateSequence.Ordinates;
            if (geometry is LineString)
                return ((LineString)geometry).CoordinateSequence.Ordinates;
            if (geometry is Polygon)
                return ((Polygon)geometry).ExteriorRing.CoordinateSequence.Ordinates;

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
        public void Write(MultiLineString multiLineString, BinaryWriter writer)
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
                                                ((LineString) multiLineString[0]).CoordinateSequence.Ordinates);
            // Write LineString's index
            for (int i = 0; i < multiLineString.NumGeometries; i++)
            {
                // Write internal holes index
                var ls = ((LineString) multiLineString.GetGeometryN(i)).CoordinateSequence;
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
        public void Write(MultiPolygon multiPolygon, BinaryWriter writer)
        {
            writer.Write((int) ShapeGeometryType.Polygon);

            // Write BoundingBox
            WriteBoundingBox(multiPolygon.EnvelopeInternal, writer);

            // Write NumParts and NumPoints
            int numParts = multiPolygon.NumGeometries;              // Exterior rings count
            for (int i = 0; i < multiPolygon.NumGeometries; i++)    // Adding interior rings count
                numParts += ((Polygon) multiPolygon.GetGeometryN(i)).NumInteriorRings;

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
                var polygon = (Polygon) multiPolygon.GetGeometryN(i);
                var shell = polygon.ExteriorRing;
                Copy(shell.CoordinateSequence, 0, seq, count, shell.NumPoints);
                count += shell.NumPoints;
                if (count == multiPolygon.NumPoints)
                    break;
                writer.Write((int) count);
                for (int j = 0; j < polygon.NumInteriorRings; j++)
                {
                    var hole = (LineString) polygon.GetInteriorRingN(j);
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
        public byte[] GetBytes(Geometry geometry)
        {
            return new byte[GetBytesLength(geometry)];
        }

        /// <summary>
        /// Return correct length for Byte Stream.
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public int GetBytesLength(Geometry geometry)
        {
            if (geometry is Point)
                return SetByteStreamLength(geometry as Point);
            else if (geometry is LineString)
                return SetByteStreamLength(geometry as LineString);
            else if (geometry is Polygon)
                return SetByteStreamLength(geometry as Polygon);
            else if (geometry is MultiPoint)
                return SetByteStreamLength(geometry as MultiPoint);
            else if (geometry is MultiLineString)
                return SetByteStreamLength(geometry as MultiLineString);
            else if (geometry is MultiPolygon)
                return SetByteStreamLength(geometry as MultiPolygon);
            else if (geometry is GeometryCollection)
                throw new NotSupportedException("GeometryCollection not supported!");
            else throw new ArgumentException("ShouldNeverReachHere!");
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="multiPolygon"></param>
        /// <returns></returns>
        protected int SetByteStreamLength(MultiPolygon multiPolygon)
        {
            int numParts = multiPolygon.NumGeometries;               // Exterior rings count
            foreach (Polygon polygon in multiPolygon.Geometries)    // Adding interior rings count
                numParts += polygon.NumInteriorRings;
            int numPoints = multiPolygon.NumPoints;
            return CalculateLength(numParts, numPoints);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="multiLineString"></param>
        /// <returns></returns>
        protected int SetByteStreamLength(MultiLineString multiLineString)
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
        protected int SetByteStreamLength(MultiPoint multiPoint)
        {
            int numPoints = multiPoint.NumPoints;
            return CalculateLength(numPoints);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        protected int SetByteStreamLength(Polygon polygon)
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
        protected int SetByteStreamLength(LineString lineString)
        {
            int numPoints = lineString.NumPoints;
            return CalculateLength(1, numPoints);   // ASSERT: IndexParts.Length == 1;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        protected int SetByteStreamLength(Point point)
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
        /// Copies a section of a <see cref="CoordinateSequence"/> to another <see cref="CoordinateSequence"/>.
        /// The sequences may have different dimensions;
        /// in this case only the common dimensions are copied.
        ///</summary>
        /// <param name="src">The sequence to copy coordinates from</param>
        /// <param name="srcPos">The starting index of the coordinates to copy</param>
        /// <param name="dest">The sequence to which the coordinates should be copied to</param>
        /// <param name="destPos">The starting index of the coordinates in <see paramref="dest"/></param>
        /// <param name="length">The number of coordinates to copy</param>
        protected static void Copy(CoordinateSequence src, int srcPos, CoordinateSequence dest, int destPos, int length)
        {
            for (int i = 0; i < length; i++)
                CopyCoord(src, srcPos + i, dest, destPos + i);
        }

        ///<summary>
        /// Copies a coordinate of a <see cref="CoordinateSequence"/> to another <see cref="CoordinateSequence"/>.
        /// The sequences may contain different <see cref="Ordinates"/>; in this case only the common ordinates are copied.
        ///</summary>
        /// <param name="src">The sequence to copy coordinate from</param>
        /// <param name="srcPos">The index of the coordinate to copy</param>
        /// <param name="dest">The sequence to which the coordinate should be copied to</param>
        /// <param name="destPos">The index of the coordinate in <see paramref="dest"/></param>
        protected static void CopyCoord(CoordinateSequence src, int srcPos, CoordinateSequence dest, int destPos)
        {
            dest.SetX(destPos, src.GetX(srcPos));
            dest.SetY(destPos, src.GetY(srcPos));
            if (src.HasZ && dest.HasZ)
            {
                dest.SetZ(destPos, src.GetZ(srcPos));
            }

            if (src.HasM && dest.HasM)
            {
                dest.SetM(destPos, src.GetM(srcPos));
            }
        }
    }
}
