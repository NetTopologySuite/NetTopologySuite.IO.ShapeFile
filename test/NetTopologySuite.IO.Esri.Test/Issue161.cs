using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Shapefile;
using NUnit.Framework;
using System.IO;

namespace NetTopologySuite.IO.ShapeFile.Test
{
    class Issue161
    {
        [Test(Description =
            "ShapefileDataReader error 'The output char buffer is too small to contain the decoded characters'")]
        public void TestIssue161()
        {
            //SETUP
            string filePath = Path.Combine(CommonHelpers.TestShapefilesDirectory, "LSOA_2011_EW_BGC.shp");
            if (!File.Exists(filePath)) Assert.Ignore("File '{0}' not present", filePath);

            //ATTEMPT
            using (var reader = ShapefileReader.Open(filePath))
            {
                while (reader.Read(out var geometry, out var attributes, out var deleted))//&& count++ < 3)
                {
                    object val;
                    Assert.DoesNotThrow(() => val = attributes["LSOA11CD"]);
                }
            }
        }
    }
}
