using NUnit.Framework;
using System.IO;
using NetTopologySuite.Geometries;
using System;

namespace NetTopologySuite.IO.ShapeFile.Test
{
    [TestFixture]
    [ShapeFileIssueNumber(70)]
    public class Issue70Fixture
    {
        /// <summary>
        /// <see href="https://github.com/NetTopologySuite/NetTopologySuite.IO.ShapeFile/issues/70"/>
        /// </summary>
        [Test]
        public void TestReadPolygonWithWrongShellOrientation()
        {
            string filePath = Path.Combine(
                CommonHelpers.TestShapefilesDirectory,
                "shell_bad_ccw.shp");
            Assert.That(File.Exists(filePath), Is.True);
            string filePathWoExt = Path.Combine(
                Path.GetDirectoryName(filePath),
                Path.GetFileNameWithoutExtension(filePath));
            using var shpReader = new ShapefileDataReader(
                filePathWoExt,
                GeometryFactory.Default);
            bool success = shpReader.Read();
            Assert.That(success, Is.True);
            var geom = shpReader.Geometry;
            Assert.That(geom, Is.Not.Null);
            Console.WriteLine(geom.AsText());
            Assert.That(geom, Is.InstanceOf<Polygon>());
            var poly = (Polygon)geom;
            Assert.That(poly.Shell, Is.Not.Null);
            Assert.That(poly.Holes, Is.Not.Null);
            Assert.That(poly.Holes.Length, Is.EqualTo(1));
        }
    }
}
