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
    public class CoreReadShapefile2 : Test
    {
        public override void Run()
        {
            var shpPath = GetTestFilePath("arcmap/shp/pt_utf8.shp");

            using (var shp = ShapefileReader.Open(shpPath))
            {
                Console.WriteLine(shp.ShapeType);
                foreach (var field in shp.Fields)
                {
                    Console.WriteLine(field);
                }

                while (shp.Read(out var deleted))
                {
                    if (deleted)
                        continue;

                    Console.WriteLine("Record ID: " + shp.Fields["Id"].Value);
                    foreach (var field in shp.Fields)
                    {
                        Console.WriteLine($"  {field.Name}: {field.Value}");
                    }
                    Console.WriteLine($"  SHAPE: {shp.Shape}");
                }
            }
        }
    }
}
