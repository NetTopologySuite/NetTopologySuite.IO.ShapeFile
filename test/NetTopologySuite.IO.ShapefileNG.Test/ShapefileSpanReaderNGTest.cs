using System;
using System.IO;
using NetTopologySuite.IO.ShapeRecords;
using NUnit.Framework;

namespace NetTopologySuite.IO
{
    public sealed class ShapefileSpanReaderNGTest
    {
        [Test]
        public void BasicPointsTest()
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

        [Test]
        public void BasicMultiPointsTest()
        {
            byte[] mainFile = File.ReadAllBytes(Path.Combine(CommonHelpers.TestShapefilesDirectory, "multipoints.shp"));
            byte[] indexFile = File.ReadAllBytes(Path.Combine(CommonHelpers.TestShapefilesDirectory, "multipoints.shx"));
            var reader = new ShapefileSpanReaderNG(mainFile, indexFile);
            Assert.That(reader.RecordCount, Is.EqualTo(2));

            var mp1 = reader.GetMultiPointXYRecord(0);
            Assert.That(mp1.Points.Length, Is.EqualTo(6));
            Assert.That(mp1.Points[0], Is.EqualTo(new PointXYRecordNG(-83.163443, 42.462961)));
            Assert.That(mp1.Points[1], Is.EqualTo(new PointXYRecordNG(-83.149998, 42.465838)));
            Assert.That(mp1.Points[2], Is.EqualTo(new PointXYRecordNG(-83.152076, 42.457263)));
            Assert.That(mp1.Points[3], Is.EqualTo(new PointXYRecordNG(-83.168934, 42.465194)));
            Assert.That(mp1.Points[4], Is.EqualTo(new PointXYRecordNG(-83.170986, 42.457793)));
            Assert.That(mp1.Points[5], Is.EqualTo(new PointXYRecordNG(-83.180223, 42.465100)));

            var mp2 = reader.GetMultiPointXYRecord(1);
            Assert.That(mp2.Points.Length, Is.EqualTo(6));
            Assert.That(mp2.Points[0], Is.EqualTo(new PointXYRecordNG(-83.155848, 42.466008)));
            Assert.That(mp2.Points[1], Is.EqualTo(new PointXYRecordNG(-83.162570, 42.459781)));
            Assert.That(mp2.Points[2], Is.EqualTo(new PointXYRecordNG(-83.163186, 42.466671)));
            Assert.That(mp2.Points[3], Is.EqualTo(new PointXYRecordNG(-83.168061, 42.460689)));
            Assert.That(mp2.Points[4], Is.EqualTo(new PointXYRecordNG(-83.151307, 42.462658)));
            Assert.That(mp2.Points[5], Is.EqualTo(new PointXYRecordNG(-83.142532, 42.460462)));
        }
    }
}
