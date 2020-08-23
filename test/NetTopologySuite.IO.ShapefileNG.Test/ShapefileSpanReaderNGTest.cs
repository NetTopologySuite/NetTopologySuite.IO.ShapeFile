using System.IO;
using NUnit.Framework;

namespace NetTopologySuite.IO
{
    public sealed class ShapefileSpanReaderNGTest
    {
        [Test]
        public void BasicTest()
        {
            byte[] mainFile = File.ReadAllBytes(Path.Combine(CommonHelpers.TestShapefilesDirectory, "points.shp"));
            byte[] indexFile = File.ReadAllBytes(Path.Combine(CommonHelpers.TestShapefilesDirectory, "points.shx"));
            var reader = new ShapefileSpanReaderNG(mainFile, indexFile);
            Assert.That(reader.RecordCount, Is.EqualTo(2));

            var dodgePark = reader.GetPointXYRecord(0);
            Assert.That(dodgePark.X, Is.EqualTo(-83.01107));
            Assert.That(dodgePark.Y, Is.EqualTo(42.59286));

            var joeLouis = reader.GetPointXYRecord(1);
            Assert.That(joeLouis.X, Is.EqualTo(-83.05270));
            Assert.That(joeLouis.Y, Is.EqualTo(42.32529));
        }
    }
}
