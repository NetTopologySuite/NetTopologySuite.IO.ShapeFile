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
    public class CoreReadShapefile1 : Test
    {
        public override void Run()
        {
            var shpPath = GetTestFilePath("arcmap/shp/pt_utf8.shp");

            using (var shp = new ShapefilePointReader(shpPath))
            {
                Console.WriteLine(shp.ShapeType);
                foreach (var field in shp.Fields)
                {
                    Console.WriteLine(field);
                }

                foreach (var feature in shp)
                {
                    Console.WriteLine("Record ID: " + feature.Attributes["Id"]);
                    foreach (var attr in feature.Attributes)
                    {
                        Console.WriteLine($"  {attr.Key}: {attr.Value}");
                    }
                    Console.WriteLine($"  SHAPE: {feature.Shape}");
                }
            }
        }
    }
}
