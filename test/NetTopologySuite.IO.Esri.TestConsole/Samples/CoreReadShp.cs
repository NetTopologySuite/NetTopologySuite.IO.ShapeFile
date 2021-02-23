using NetTopologySuite.IO.Dbf;
using NetTopologySuite.IO.Shapefile.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTopologySuite.IO.Esri.TestConsole.Tests
{
    public class CoreReadShp : Test
    {
        public override void Run()
        {
            var shpPath = GetTestFilePath("arcmap/shp/pt_utf8.shp");

            using (var shpStream = File.OpenRead(shpPath))
            using (var shp = new ShpPointReader(shpStream))
            {
                Console.WriteLine(shp.ShapeType);
                while (shp.Read())
                {
                    Console.WriteLine(shp.Shape);
                }
            }
        }
    }
}
