using NetTopologySuite.IO.Dbf;
using NetTopologySuite.IO.Shapefile;
using NetTopologySuite.IO.Shapefile.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTopologySuite.IO.Esri.TestConsole.Tests
{
    public class CoreWriteShapefile1 : Test
    {
        public override void Run()
        {
            var shpPath = GetTempFilePath("abcd1.shp");

            var features = new List<ShapefileFeature>();
            for (int i = 1; i < 5; i++)
            {
                var attributes = new Dictionary<string, object>();
                attributes["date"] = new DateTime(2000, 1, i + 1);
                attributes["float"] = i * 0.1;
                attributes["int"] = i;
                attributes["logical"] = i % 2 == 0;
                attributes["text"] = i.ToString("0.00");

                var line = new List<ShpCoordinates>();
                line.Add(new ShpCoordinates(i, i + 1, i));
                line.Add(new ShpCoordinates(i, i, i));
                line.Add(new ShpCoordinates(i + 1, i, i));

                var shapeParts = new List<List<ShpCoordinates>>();
                shapeParts.Add(line);

                var feature = new ShapefileFeature(shapeParts, attributes);
                features.Add(feature);
            }

            var dateField = DbfField.Create("date", typeof(DateTime));
            var floatField = DbfField.Create("float", typeof(double));
            var intField = DbfField.Create("int", typeof(int));
            var LogicalField = DbfField.Create("logical", typeof(bool));
            var textField = DbfField.Create("text", typeof(string));

            using (var shp = new ShapefileMultiPartWriter(shpPath, ShapeType.PolyLine, dateField, floatField, intField, LogicalField, textField))
            {
                shp.Write(features);
            }

            foreach (var feature in Shapefile.Core.ShapefileReader.ReadAll(shpPath))
            {
                Console.WriteLine(feature.Attributes);
                Console.WriteLine(feature.Shape);
            }
        }
    }
}
