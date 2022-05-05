using NetTopologySuite.IO.Shapefile.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTopologySuite.IO.Esri.TestConsole.Tests
{
    public abstract class ArcMapShapefilesTest : Test
    {
        public override void Run()
        {
            var filePath = GetTestFilePath("arcmap/shp/point.shp");
            var shpDataDir = Path.GetDirectoryName(filePath);

            foreach (var shpFilePath in Directory.GetFiles(shpDataDir, "*.shp"))
            {
                var fileName = "arcmap/shp/" + Path.GetFileName(shpFilePath);
                //if (!fileName.Contains("pt_utf8"))
                //    continue;

                using (var reader = ShapefileReader.Open(shpFilePath))
                {
                    RunShapefile(fileName, reader);
                }
            }
        }

        protected abstract void RunShapefile(string fileName, ShapefileReader shp);
    }
}
