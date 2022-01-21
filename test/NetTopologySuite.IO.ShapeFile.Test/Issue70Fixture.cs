using NUnit.Framework;
using System.IO;
using NetTopologySuite.Geometries;
using System;
using System.Linq;

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
            /*
             * The shell_bad_ccw.shp contains a single polygon, with:
             *  - a shell CCW-oriented (like a hole from ESRI specs
             *  - a hole CW-oriented (like a shell from ESRI specs)
             */
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
            Assert.That(geom.IsValid, Is.True);
            Assert.That(geom.NumGeometries, Is.EqualTo(1));
            Assert.That(geom, Is.InstanceOf<Polygon>());
            var poly = (Polygon)geom.GetGeometryN(0);
            Assert.That(poly.Shell, Is.Not.Null);
            Assert.That(poly.Holes, Is.Not.Null);
            Assert.That(poly.Holes.Length, Is.EqualTo(1));
        }
    }
}
