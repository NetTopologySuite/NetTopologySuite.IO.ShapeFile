using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using NetTopologySuite.IO.Shapefile;

namespace NetTopologySuite.IO.ShapeFile.Test
{
    [TestFixture]
    [ShapeFileIssueNumber(60)]
    public class Issue60Fixture
    {
        /// <summary>
        /// <see href="https://github.com/NetTopologySuite/NetTopologySuite.IO.ShapeFile/issues/60"/>
        /// </summary>
        /// <remarks>without fix results into System.OverflowException</remarks>
        [Test]
        public void Feature_without_fields_should_be_written_correctly()
        {
            // They should not.
            // https://desktop.arcgis.com/en/arcmap/latest/manage-data/shapefiles/geoprocessing-considerations-for-shapefile-output.htm
            // The dBASE file must contain at least one field. When you create a new shapefile or dBASE table, an integer ID field is created as a default.

            /*
            string test56 = Path.Combine(CommonHelpers.TestShapefilesDirectory, "test60.shp");
            var factory = new GeometryFactory();
            var attributes = new AttributesTable();
            var feature = new Feature(factory.CreatePoint(new Coordinate(1, 2)), attributes);
            var features = new[] { feature };

            features.SaveToShapefile(test56);

            foreach (var readFeature in ShapefileReader.ReadAll(test56))
            {
                Assert.AreEqual(feature.Geometry.AsText(), readFeature.Geometry.AsText());
            }
            */
        }

    }
}
