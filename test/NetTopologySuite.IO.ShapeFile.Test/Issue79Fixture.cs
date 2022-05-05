﻿using NUnit.Framework;
using System.IO;
using NetTopologySuite.Geometries;

namespace NetTopologySuite.IO.ShapeFile.Test
{
    [TestFixture]
    [ShapeFileIssueNumber(79)]
    public class Issue79Fixture
    {
        /// <summary>
        /// <see href="https://github.com/NetTopologySuite/NetTopologySuite.IO.ShapeFile/issues/79"/>
        /// </summary>
        [Test]
        public void TestReadEmptyShapefile()
        {
            string filePath = Path.Combine(
                CommonHelpers.TestShapefilesDirectory,
                "__emptyShapefile.shp");
            Assert.That(File.Exists(filePath), Is.True);
            string filePathWoExt = Path.Combine(
                Path.GetDirectoryName(filePath),
                Path.GetFileNameWithoutExtension(filePath));
            using var shpReader = new ShapefileDataReader(
                filePathWoExt,
                GeometryFactory.Default);
            bool success = shpReader.Read();
            Assert.That(success, Is.False);
        }
    }
}
