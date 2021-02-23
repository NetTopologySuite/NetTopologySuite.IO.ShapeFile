using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
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
    public class NtsWriteShapefile1 : Test
    {
        public override void Run()
        {
            var shpPath = GetTempFilePath("abcd3.shp");

            var features = new List<Feature>();
            for (int i = 1; i < 5; i++)
            {
                var attributes = new AttributesTable();
                attributes.Add("date", new DateTime(2000, 1, i + 1));
                attributes.Add("float", i * 0.1);
                attributes.Add("int", i);
                attributes.Add("logical", i % 2 == 0);
                attributes.Add("text", i.ToString("0.00"));

                var lineCoords = new List<CoordinateZ>();
                lineCoords.Add(new CoordinateZ(i, i + 1, i));
                lineCoords.Add(new CoordinateZ(i, i, i));
                lineCoords.Add(new CoordinateZ(i + 1, i, i));
                var line = new LineString(lineCoords.ToArray());

                var feature = new Feature(line, attributes);
                features.Add(feature);
            }

            features.SaveToShapefile(shpPath);
        }
    }
}
