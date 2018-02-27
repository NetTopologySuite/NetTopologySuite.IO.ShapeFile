using NetTopologySuite.Geometries;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetTopologySuite.IO.ShapeFile.Test
{
    class Issue161
    {
        [Test(Description =
            "ShapefileDataReader error 'The output char buffer is too small to contain the decoded characters'")]
        public void TestIssue161()
        {
            var testFileDataDirectory = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    string.Format("..{0}..{0}..{0}NetTopologySuite.Samples.Shapefiles", Path.DirectorySeparatorChar));

            //SETUP
            var filePath = Path.Combine(testFileDataDirectory, "LSOA_2011_EW_BGC.shp");
            if (!File.Exists(filePath)) Assert.Ignore("File '{0}' not present", filePath);

            //ATTEMPT
            using (var reader = new ShapefileDataReader(filePath, GeometryFactory.Default))
            {
                var header = reader.ShapeHeader;

                while (reader.Read())//&& count++ < 3)
                {
                    object val;
                    Assert.DoesNotThrow(() => val = reader["LSOA11CD"]);
                }
            }
        }
    }
}
