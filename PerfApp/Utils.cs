using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.Collections.Generic;
using System.IO;

namespace PerfApp
{
    internal static class Utils
    {
        internal static IEnumerable<IFeature> CreateFeatures(GeometryFactory fac, uint count, uint step)
        {
            var list = new List<Polygon>();
            int counter = 0;
            int indexer = 0;
            for (uint i = 1; i < count * 10; i += step)
            {
                var shell = fac.CreateLinearRing(new Coordinate[]
                {
                    new Coordinate(1 * i, 1 * i),
                    new Coordinate(9 * i, 1 * i),
                    new Coordinate(9 * i, 9* i),
                    new Coordinate(1 * i, 9* i),
                    new Coordinate(1 * i, 1* i),
                });
                var hole1 = fac.CreateLinearRing(new Coordinate[]
                {
                    new Coordinate(2* i, 2* i),
                    new Coordinate(3* i, 3* i),
                    new Coordinate(4* i, 2* i),
                    new Coordinate(2* i, 2* i),
                });
                var hole2 = fac.CreateLinearRing(new Coordinate[]
                {
                    new Coordinate(6* i, 6* i),
                    new Coordinate(8* i, 8* i),
                    new Coordinate(7* i, 6* i),
                    new Coordinate(6* i, 6* i),
                });
                var poly = fac.CreatePolygon(shell, new[] { hole1, hole2 });
                list.Add(poly);
                if (++counter >= 100)
                {
                    var mpoly = fac.CreateMultiPolygon(list.ToArray());
                    var attrs = new AttributesTable { { "id", ++indexer } };
                    yield return new Feature(mpoly, attrs);

                    list.Clear();
                    counter = 0;
                }
            }
            if (list.Count != 0)
            {
                var mpoly1 = fac.CreateMultiPolygon(list.ToArray());
                var attrs1 = new AttributesTable { { "id", ++indexer } };
                yield return new Feature(mpoly1, attrs1);
            }
        }

        internal static string WriteFeatures(IList<IFeature> features)
        {
            string fname = Path.ChangeExtension(Path.GetTempFileName(), ".shp");
            var header = ShapefileDataWriter.GetHeader(features[0], features.Count);
            var writer = new ShapefileDataWriter(fname) { Header = header };
            writer.Write(features);
            return Path.Combine(
                Path.GetDirectoryName(fname),
                Path.GetFileNameWithoutExtension(fname));
        }
    }
}
