using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace NetTopologySuite.IO.Shapefile.Core
{


    internal static class ShpBinaryExtensions
    {
        public static void WriteShpFileHeader(this BinaryBufferWriter binary, ShapeType type, int fileLength, ShpBoundingBox extent, bool hasZ, bool hasM)
        {
            binary.WriteInt32BigEndian(Shapefile.FileCode);
            binary.WriteBytes(byte.MinValue, 20);

            binary.Write16BitWords(fileLength);  // in 16-bit words, including the header
            binary.WriteInt32LittleEndian(Shapefile.Version);
            binary.WriteGeometryType(type);

            binary.WriteXYBoundingBox(extent);
            binary.WriteZRange(extent, hasZ);
            binary.WriteMRange(extent, hasM);
        }

        public static void ReadShpFileHeader(this BinaryBufferReader binary, out ShapeType type, out int fileLength, ShpBoundingBox boundingBox)
        {
            var fileCode = binary.ReadInt32BigEndian();
            binary.Advance(20);

            fileLength = binary.Read16BitWords();  // in 16-bit words, including the header
            var version = binary.ReadInt32LittleEndian();
            type = binary.ReadGeometryType();

            binary.ReadXYBoundingBox(boundingBox);
            binary.ReadZRange(boundingBox, type.HasZ());
            binary.ReadMRange(boundingBox, type.HasM());

            if (fileCode != Shapefile.FileCode)
                throw new FileLoadException("Invalid shapefile format.");


            Debug.Assert(version == Shapefile.Version, "Shapefile version", $"Ivalid SHP version: {version} (expected: 1000).");
        }


        public static void WriteShpRecordHeader(this BinaryBufferWriter shpRecordHeader, int recrodNumber, int contentLength)
        {
            shpRecordHeader.WriteInt32BigEndian(recrodNumber);
            shpRecordHeader.Write16BitWords(contentLength);
        }
        public static void ReadShpRecordHeader(this BinaryBufferReader shpRecordHeader, out int recrodNumber, out int contentLength)
        {
            recrodNumber = shpRecordHeader.ReadInt32BigEndian();
            contentLength = shpRecordHeader.Read16BitWords();
        }


        public static void WriteGeometryType(this BinaryBufferWriter binary, ShapeType type)
        {
            binary.WriteInt32LittleEndian((int)type);
        }
        public static ShapeType ReadGeometryType(this BinaryBufferReader binary)
        {
            return (ShapeType)binary.ReadInt32LittleEndian();
        }


        public static void ReadXYCoordinate(this BinaryBufferReader shpRecordData, out double x, out double y)
        {
            x = shpRecordData.ReadDoubleLittleEndian();
            y = shpRecordData.ReadDoubleLittleEndian();
        }
        private static void WriteXYCoordinate(this BinaryBufferWriter shpRecordData, double x, double y)
        {
            // Avoid performance costs (if you trying to pas NaN as X,Y then you're wrong).
            // x = x.ToValidShpOrdinate(0.0);
            // x = x.ToValidShpOrdinate(0.0);

            shpRecordData.WriteDoubleLittleEndian(x);
            shpRecordData.WriteDoubleLittleEndian(y);
        }
        public static void WriteXYOrdinate(this BinaryBufferWriter shpRecordData, double x, double y, ShpBoundingBox shpExtent)
        {
            shpRecordData.WriteXYCoordinate(x, y);
            shpExtent.X.Expand(x);
            shpExtent.Y.Expand(y);
        }



        private static void ReadXYCoordinates(this BinaryBufferReader shpRecordData, int count, ShpShapeBuilder points)
        {
            for (int i = 0; i < count; i++)
            {
                shpRecordData.ReadXYCoordinate(out var x, out var y);
                points.AddPoint(x, y);
            }
        }
        public static void WriteXYCoordinates(this BinaryBufferWriter shpRecordData, ShpShapeBuilder points)
        {
            for (int i = 0; i < points.PointCount; i++)
            {
                shpRecordData.WriteXYCoordinate(points[i].X, points[i].Y);
            }
        }


        public static double ReadZCoordinate(this BinaryBufferReader shpRecordData)
        {
            return shpRecordData.ReadDoubleLittleEndian();
        }
        private static void WriteZCoordinate(this BinaryBufferWriter shpRecordData, double z)
        {
            shpRecordData.WriteDoubleLittleEndian(z.ToValidShpCoordinate());
        }


        private static void AdvancePastZRange(this BinaryBufferReader binary)
        {
            binary.Advance(2 * sizeof(double));
        }
        private static void ReadZRange(this BinaryBufferReader binary, ShpBoundingBox shpExtent, bool hasZ)
        {
            if (hasZ)
            {
                shpExtent.Z.Expand(binary.ReadZCoordinate());
                shpExtent.Z.Expand(binary.ReadZCoordinate());
            }
        }
        public static void WriteZRange(this BinaryBufferWriter binary, ShpBoundingBox shpExtent, bool hasZ)
        {
            if (hasZ)
            {
                binary.WriteZCoordinate(shpExtent.Z.Min);
                binary.WriteZCoordinate(shpExtent.Z.Max);
            }
            else
            {
                binary.WriteZCoordinate(0.0); // ArcMap uses zero as default.
                binary.WriteZCoordinate(0.0);
            }
        }


        private static void ReadZCoordinates(this BinaryBufferReader shpRecordData, int count, ShpShapeBuilder points)
        {
            for (int i = 0; i < count; i++)
            {
                points.Points[i].Z = shpRecordData.ReadZCoordinate();
            }
        }
        public static void WriteZCoordinates(this BinaryBufferWriter shpRecordData, ShpShapeBuilder points)
        {
            for (int i = 0; i < points.PointCount; i++)
            {
                shpRecordData.WriteZCoordinate(points.Points[i].Z);
            }
        }


        public static double ReadMValue(this BinaryBufferReader shpRecordData)
        {
            var m = shpRecordData.ReadDoubleLittleEndian();
            if (m < Shapefile.MeasureMinValue)
                return double.NaN;

            return m;
        }
        private static void WriteMValue(this BinaryBufferWriter shpRecordData, double m)
        {
            shpRecordData.WriteDoubleLittleEndian(m.ToValidShpMeasure());
        }


        private static void AdvancePastMRange(this BinaryBufferReader binary)
        {
            binary.Advance(2 * sizeof(double));
        }
        private static void ReadMRange(this BinaryBufferReader binary, ShpBoundingBox shpExtent, bool hasM)
        {
            if (hasM)
            {
                shpExtent.M.Expand(binary.ReadMValue());
                shpExtent.M.Expand(binary.ReadMValue());
            }
        }
        public static void WriteMRange(this BinaryBufferWriter binary, ShpBoundingBox shpExtent, bool hasM)
        {
            if (hasM)
            {
                binary.WriteMValue(shpExtent.M.Min);
                binary.WriteMValue(shpExtent.M.Max);
            }
            else
            {
                binary.WriteMValue(0.0); // ArcMap uses zero as default.
                binary.WriteMValue(0.0);
            }
        }


        private static void ReadMValues(this BinaryBufferReader shpRecordData, int count, ShpShapeBuilder points)
        {
            for (int i = 0; i < count; i++)
            {
                points.Points[i].M = shpRecordData.ReadMValue();

            }
        }
        public static void WriteMValues(this BinaryBufferWriter shpRecordData, ShpShapeBuilder points)
        {
            for (int i = 0; i < points.PointCount; i++)
            {
                shpRecordData.WriteMValue(points.Points[i].M);
            }
        }

        public static void AdvancePastXYBoundingBox(this BinaryBufferReader boundingBoxBinary)
        {
            boundingBoxBinary.Advance(4 * sizeof(double));
        }
        public static void ReadXYBoundingBox(this BinaryBufferReader boundingBoxBinary, ShpBoundingBox boundingBox)
        {
            boundingBoxBinary.ReadXYCoordinate(out var xMin, out var yMin);
            boundingBox.X.Expand(xMin);
            boundingBox.Y.Expand(yMin);

            boundingBoxBinary.ReadXYCoordinate(out var xMax, out var yMax);
            boundingBox.X.Expand(xMax);
            boundingBox.Y.Expand(yMax);
        }
        public static void WriteXYBoundingBox(this BinaryBufferWriter boundingBoxBinary, ShpBoundingBox shpExtent)
        {
            boundingBoxBinary.WriteXYCoordinate(shpExtent.X.Min.ToValidShpCoordinate(), shpExtent.Y.Min.ToValidShpCoordinate());
            boundingBoxBinary.WriteXYCoordinate(shpExtent.X.Max.ToValidShpCoordinate(), shpExtent.Y.Max.ToValidShpCoordinate());
        }


        public static int ReadPartCount(this BinaryBufferReader binary)
        {
            return binary.ReadInt32LittleEndian();  
        }
        public static void WritePartCount(this BinaryBufferWriter binary, int count)
        {
            binary.WriteInt32LittleEndian(count);
        }


        public static int ReadPointCount(this BinaryBufferReader binary)
        {
            return binary.ReadInt32LittleEndian();
        }
        public static void WritePointCount(this BinaryBufferWriter binary, int count)
        {
            binary.WriteInt32LittleEndian(count);
        }


        public static void ReadPartOfsets(this BinaryBufferReader binary, int partCount, ShpShapeBuilder points)
        {
            for (int partIndex = 0; partIndex < partCount; partIndex++)
            {
                points.AddPartOffset(binary.ReadInt32LittleEndian());
            }
        }
        public static void WritePartOffsets(this BinaryBufferWriter binary, ShpShapeBuilder points)
        {
            for (int partIndex = 0; partIndex < points.PartCount; partIndex++)
            {
                binary.WriteInt32LittleEndian(points.GetPartOffset(partIndex));
            }
        }


        public static void ReadPoint(this BinaryBufferReader shpRecordData, bool hasZ, bool hasM, ref ShpCoordinates point)
        {
            shpRecordData.ReadXYCoordinate(out point.X, out point.Y);

            if (hasZ)
            {
                point.Z = shpRecordData.ReadZCoordinate();
            }
            if (hasM)
            {
                point.M = shpRecordData.ReadMValue();
            }
        }
        public static void WritePoint(this BinaryBufferWriter shpRecordData, bool hasZ, bool hasM, ShpCoordinates point)
        {
            shpRecordData.WriteXYCoordinate(point.X, point.Y);

            if (hasZ)
            {
                shpRecordData.WriteZCoordinate(point.Z);
            }

            if (hasM)
            {
                shpRecordData.WriteMValue(point.M);
            }
        }


        public static void ReadPoints(this BinaryBufferReader shpRecordData, int pointCount, bool hasZ, bool hasM, ShpShapeBuilder points)
        {
            shpRecordData.ReadXYCoordinates(pointCount, points);

            if (hasZ)
            {
                shpRecordData.AdvancePastZRange();
                shpRecordData.ReadZCoordinates(pointCount, points);
            }

            if (hasM)
            {
                shpRecordData.AdvancePastMRange();
                shpRecordData.ReadMValues(pointCount, points);
            }
        }
        public static void WritePoints(this BinaryBufferWriter shpRecordData, bool hasZ, bool hasM, ShpShapeBuilder points)
        {
            shpRecordData.WriteXYCoordinates(points);

            if (hasZ)
            {
                shpRecordData.WriteZRange(points.Extent, hasZ);
                shpRecordData.WriteZCoordinates(points);
            }

            if (hasM)
            {
                shpRecordData.WriteMRange(points.Extent, hasM);
                shpRecordData.WriteMValues(points);
            }
        }

    }



}
