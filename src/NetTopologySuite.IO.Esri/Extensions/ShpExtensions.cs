using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Text;
using NetTopologySuite.IO.Shapefile.Core;

namespace NetTopologySuite.IO.Shapefile
{
    internal static class ShpExtensions
    {
        public static Point ToPoint(this ShpCoordinates shpPoint, bool hasZ, bool hasM)
        {
            if (hasZ && hasM)
            {
                return new Point(shpPoint.X, shpPoint.Y, shpPoint.Z) { M = shpPoint.M };
            }
            else if (hasZ)
            {
                return new Point(shpPoint.X, shpPoint.Y, shpPoint.Z);
            }
            else if (hasM)
            {
                return new Point(shpPoint.X, shpPoint.Y) { M = shpPoint.M };
            }
            else
            {
                return new Point(shpPoint.X, shpPoint.Y);
            }
        }


        public static void SetCoordinates(this CoordinateSequence sequence, int index, ShpCoordinates coords, bool hasZ, bool hasM)
        {
            sequence.SetX(index, coords.X);
            sequence.SetY(index, coords.Y);

            if (hasZ)
                sequence.SetZ(index, coords.Z);

            if (hasM)
                sequence.SetM(index, coords.M);
        }


        public static CoordinateSequence GetPartCoordinates(this ShpShapeBuilder shape, int partIndex, bool hasZ, bool hasM, bool closeSequence = false)
        {
            var partOffset = shape.GetPartOffset(partIndex);
            var shpPartPointCount = shape.GetPointCount(partIndex);

            if (closeSequence)
            {
                var firstPoint = shape[partOffset];
                var lastPoint = shape[partOffset + shpPartPointCount - 1];

                closeSequence = (shpPartPointCount > 2) && !firstPoint.Equals(lastPoint);
            }

            var sequencePointCount = closeSequence ? shpPartPointCount + 1 : shpPartPointCount;
            var points = CreateCoordinateSequence(sequencePointCount, hasZ, hasM);

            for (int i = 0; i < shpPartPointCount; i++)
            {
                var p = shape[partOffset + i];
                points.SetCoordinates(i, p, hasZ, hasM);
            }

            if (closeSequence)
            {
                var firstPoint = shape[partOffset];
                var lastPointIndex = sequencePointCount - 1;
                points.SetCoordinates(lastPointIndex, firstPoint, hasZ, hasM);
            }

            return points;
        }


        private static CoordinateSequence CreateCoordinateSequence(int size, bool hasZ, bool hasM)
        {
            if (hasZ && hasM)
                return GeometryFactory.Default.CoordinateSequenceFactory.Create(size, Ordinates.XYZM);

            if (hasZ)
                return GeometryFactory.Default.CoordinateSequenceFactory.Create(size, Ordinates.XYZ);

            if (hasM)
                return GeometryFactory.Default.CoordinateSequenceFactory.Create(size, Ordinates.XYM);

           return GeometryFactory.Default.CoordinateSequenceFactory.Create(size, Ordinates.XY);
        }


        internal static void AddPoint(this ShpShapeBuilder shape, Point point)
        {
            if (point == null || point == Point.Empty)
                return;

            shape.AddPoint(point.X, point.Y, point.Z, point.M);
        }


        internal static void AddPart(this ShpShapeBuilder shape, CoordinateSequence coordinates)
        {
            shape.StartNewPart();

            for (int i = 0; i < coordinates.Count; i++)
            {
                var x = coordinates.GetX(i);
                var y = coordinates.GetY(i);
                var z = coordinates.GetZ(i);
                var m = coordinates.GetM(i);
                shape.AddPoint(x, y, z, m);
            }
        }

    }



}
