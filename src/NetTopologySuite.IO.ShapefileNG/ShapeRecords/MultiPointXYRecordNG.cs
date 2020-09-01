using System;

namespace NetTopologySuite.IO.ShapeRecords
{
    public readonly ref struct MultiPointXYRecordNG
    {
        public readonly double MinX;

        public readonly double MinY;

        public readonly double MaxX;

        public readonly double MaxY;

        public readonly ReadOnlySpan<PointXYRecordNG> Points;

        public MultiPointXYRecordNG(double minX, double minY, double maxX, double maxY, ReadOnlySpan<PointXYRecordNG> points)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
            Points = points;
        }
    }
}
