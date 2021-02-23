using NetTopologySuite.IO.Dbf;
using NetTopologySuite.IO.Shapefile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTopologySuite.IO.Esri.TestConsole.Tests
{
    public class NtsReadShapefile : Test
    {
        public override void Run()
        {
            var shpPath = GetTestFilePath("arcmap/shp/pt_utf8.shp");

            foreach (var feature in ShapefileReader.ReadAll(shpPath))
            {
                Console.WriteLine("Record ID: " + feature.Attributes["Id"]);
                foreach (var attrName in feature.Attributes.GetNames())
                {
                    Console.WriteLine($"  {attrName}: {feature.Attributes[attrName]}");
                }
                Console.WriteLine($"  SHAPE: {feature.Geometry}");
            }
        }
    }
}
