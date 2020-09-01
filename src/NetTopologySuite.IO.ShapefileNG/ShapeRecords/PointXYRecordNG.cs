using System;
using System.Runtime.InteropServices;

namespace NetTopologySuite.IO.ShapeRecords
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct PointXYRecordNG : IEquatable<PointXYRecordNG>
    {
        public readonly double X;

        public readonly double Y;

        public PointXYRecordNG(double x, double y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object obj)
            => obj is PointXYRecordNG other && Equals(other);

        public bool Equals(PointXYRecordNG other)
            => X.Equals(other.X) && Y.Equals(other.Y);

        public override int GetHashCode()
            => (X, Y).GetHashCode();

        public override string ToString()
            => $"({X}, {Y})";
    }
}
