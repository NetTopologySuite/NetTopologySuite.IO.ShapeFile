using System.Runtime.InteropServices;

namespace NetTopologySuite.IO.ShapeRecords
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PointXYRecordNG
    {
        public double X;

        public double Y;

        public PointXYRecordNG(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}
