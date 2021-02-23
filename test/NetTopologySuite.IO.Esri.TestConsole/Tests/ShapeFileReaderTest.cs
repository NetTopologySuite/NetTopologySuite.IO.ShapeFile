using NetTopologySuite.IO.Shapefile.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTopologySuite.IO.Esri.TestConsole.Tests
{
    public class ShapeFileReaderTest : DbaseRreaderTest
    {

        public ShapeFileReaderTest(string path) : base(path)
        {
            Title = "Read SHP file";
        }

        public override void Run()
        {
            var fullPath = GetTestFilePath(Path);

            using (var shp = new ShapefilePointReader(fullPath))
            {
                WriteFeatures(shp);
            }
        }
    }
}
