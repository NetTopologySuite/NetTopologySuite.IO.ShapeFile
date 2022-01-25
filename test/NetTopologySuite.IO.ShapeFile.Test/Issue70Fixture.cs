﻿using NUnit.Framework;
using System.IO;
using NetTopologySuite.Geometries;
using System;
using System.Linq;
using System.Collections.Generic;

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

        private const string WktMultiPoly = @"
MULTIPOLYGON (((-124.134 -79.199, -124.141 -79.316, -124.164 -79.431, -124.202 -79.542, -124.254 -79.647, -124.319 -79.745, -124.396 -79.833, -124.484 -79.91, -124.582 -79.975, -124.687 -80.027, -124.798 -80.065, -124.913 -80.088, -125.03 -80.095, -125.147 -80.088, -125.262 -80.065, -125.373 -80.027, -125.478 -79.975, -125.576 -79.91, -125.664 -79.833, -125.741 -79.745, -125.806 -79.647, -125.858 -79.542, -125.896 -79.431, -125.919 -79.316, -125.926 -79.199, -125.919 -79.082, -125.896 -78.967, -125.858 -78.856, -125.806 -78.751, -125.741 -78.653, -125.664 -78.565, -125.576 -78.488, -125.478 -78.423, -125.373 -78.371, -125.262 -78.333, -125.147 -78.31, -125.03 -78.303, -124.913 -78.31, -124.798 -78.333, -124.687 -78.371, -124.582 -78.423, -124.484 -78.488, -124.396 -78.565, -124.319 -78.653, -124.254 -78.751, -124.202 -78.856, -124.164 -78.967, -124.141 -79.082, -124.134 -79.199),(-124.438 -79.199, -124.443 -79.122, -124.459 -79.046, -124.483 -78.973, -124.518 -78.903, -124.561 -78.839, -124.612 -78.781, -124.67 -78.73, -124.734 -78.687, -124.804 -78.652, -124.877 -78.628, -124.953 -78.612, -125.03 -78.607, -125.107 -78.612, -125.183 -78.628, -125.256 -78.652, -125.326 -78.687, -125.39 -78.73, -125.448 -78.781, -125.499 -78.839, -125.542 -78.903, -125.577 -78.973, -125.601 -79.046, -125.617 -79.122, -125.622 -79.199, -125.617 -79.276, -125.601 -79.352, -125.577 -79.425, -125.542 -79.495, -125.499 -79.559, -125.448 -79.617, -125.39 -79.668, -125.326 -79.711, -125.256 -79.746, -125.183 -79.77, -125.107 -79.786, -125.03 -79.791, -124.953 -79.786, -124.877 -79.77, -124.804 -79.746, -124.734 -79.711, -124.67 -79.668, -124.612 -79.617, -124.561 -79.559, -124.518 -79.495, -124.483 -79.425, -124.459 -79.352, -124.443 -79.276, -124.438 -79.199)),((-124.582 -79.199, -124.586 -79.257, -124.597 -79.315, -124.616 -79.371, -124.642 -79.423, -124.674 -79.472, -124.713 -79.516, -124.757 -79.555, -124.806 -79.587, -124.858 -79.613, -124.914 -79.632, -124.972 -79.643, -125.03 -79.647, -125.088 -79.643, -125.146 -79.632, -125.202 -79.613, -125.254 -79.587, -125.303 -79.555, -125.347 -79.516, -125.386 -79.472, -125.418 -79.423, -125.444 -79.371, -125.463 -79.315, -125.474 -79.257, -125.478 -79.199, -125.474 -79.141, -125.463 -79.083, -125.444 -79.027, -125.418 -78.975, -125.386 -78.926, -125.347 -78.882, -125.303 -78.843, -125.254 -78.811, -125.202 -78.785, -125.146 -78.766, -125.088 -78.755, -125.03 -78.751, -124.972 -78.755, -124.914 -78.766, -124.858 -78.785, -124.806 -78.811, -124.757 -78.843, -124.713 -78.882, -124.674 -78.926, -124.642 -78.975, -124.616 -79.027, -124.597 -79.083, -124.586 -79.141, -124.582 -79.199),(-124.896 -79.199, -124.897 -79.181, -124.9 -79.164, -124.906 -79.148, -124.914 -79.132, -124.923 -79.117, -124.935 -79.104, -124.948 -79.092, -124.963 -79.083, -124.979 -79.075, -124.995 -79.069, -125.012 -79.066, -125.03 -79.065, -125.048 -79.066, -125.065 -79.069, -125.081 -79.075, -125.097 -79.083, -125.112 -79.092, -125.125 -79.104, -125.137 -79.117, -125.146 -79.132, -125.154 -79.148, -125.16 -79.164, -125.163 -79.181, -125.164 -79.199, -125.163 -79.217, -125.16 -79.234, -125.154 -79.25, -125.146 -79.266, -125.137 -79.281, -125.125 -79.294, -125.112 -79.306, -125.097 -79.315, -125.081 -79.323, -125.065 -79.329, -125.048 -79.332, -125.03 -79.333, -125.012 -79.332, -124.995 -79.329, -124.979 -79.323, -124.963 -79.315, -124.948 -79.306, -124.935 -79.294, -124.923 -79.281, -124.914 -79.266, -124.906 -79.25, -124.9 -79.234, -124.897 -79.217, -124.896 -79.199)))
";

        [Test]
        public void TestValidMultiPolygonIsWrittenAndReadAsSameGeometry()
        {
            var fac = GeometryFactory.Default;
            var geom = new WKTReader(fac).Read(WktMultiPoly);
            Assert.That(geom.IsValid, Is.True);
            var coll = fac.CreateGeometryCollection(new[] { geom });
            ShapeFileDataWriterTest.DoTest(coll, Ordinates.XY);
        }

        [Test]
        public void TestPolygonWithHoleInsideAnotherHoleIsNotValid()
        {
            var fac = GeometryFactory.Default;
            var geom = new WKTReader(fac).Read(WktMultiPoly);
            Assert.That(geom.IsValid, Is.True);
            Assert.That(geom, Is.InstanceOf<MultiPolygon>());
            var mpoly = (MultiPolygon)geom;

            var rings = CollectRings()
                .OrderByDescending(p => fac.CreatePolygon(p).Area)
                .ToList();
            IEnumerable<LinearRing> CollectRings()
            {
                foreach (Polygon poly in mpoly.Geometries)
                {
                    yield return poly.Shell;
                    foreach (var hole in poly.Holes)
                        yield return hole;
                }
            }
            Assert.That(rings.Count, Is.EqualTo(4));
            for (int i = 1; i < rings.Count; i++)
            {
                var prev = rings[i - 1];
                var curr = rings[i];
                Assert.That(prev.EnvelopeInternal
                    .Contains(curr.EnvelopeInternal), Is.True);
            }

            var testPoly = fac.CreatePolygon(rings[0], rings.Skip(1).ToArray());
            Assert.That(testPoly.IsValid, Is.False);
            Assert.That(testPoly.Holes.Count, Is.EqualTo(3));
            for (int i = 0; i < testPoly.Holes.Length; i++)
            {
                var prev = i == 0 ? testPoly.Shell : testPoly.Holes[i - 1];
                var curr = testPoly.Holes[i];
                Assert.That(prev.EnvelopeInternal
                    .Contains(curr.EnvelopeInternal), Is.True);
            }
        }
    }
}
