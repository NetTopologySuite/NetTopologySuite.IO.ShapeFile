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
             *
             *  ShapefileReader reads this kind of data as two
             *  separate shells, added to a multipolygon.
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
            Assert.That(geom.IsValid, Is.False);
            Assert.That(geom, Is.InstanceOf<MultiPolygon>());
            var mpoly = (MultiPolygon)geom;
            Assert.That(mpoly.NumGeometries, Is.EqualTo(2));
            var polys = mpoly.Geometries.Cast<Polygon>();
            CollectionAssert.AllItemsAreNotNull(polys.Select(p => p.Shell));
            CollectionAssert.AllItemsAreNotNull(polys.Select(p => p.Holes));
            Assert.AreEqual(0, polys.Sum(p => p.Holes.Length));
        }
    }
}
