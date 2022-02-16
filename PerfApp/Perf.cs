using BenchmarkDotNet.Attributes;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.Linq;

namespace PerfApp
{
    public class Perf
    {
        private const int Count = 50000;
        private const int Step = 10;

        private static readonly GeometryFactory Fac = GeometryFactory.Default;

        private readonly string fname;

        public Perf()
        {
            var features = Utils.CreateFeatures(Fac, Count, Step).ToList();
            fname = Utils.WriteFeatures(features);
        }

        private int InternalRead()
        {
            int i = 0;
            var reader = Shapefile.CreateDataReader(fname, Fac);
            while (reader.Read())
                i++;
            return i;
        }

        [Benchmark]
        public int ReadWithFlagDisabled()
        {
            Shapefile.ExperimentalPolygonBuilderEnabled = false;
            return InternalRead();
        }

        [Benchmark]
        public int ReadWithFlagEnabled()
        {
            Shapefile.ExperimentalPolygonBuilderEnabled = true;
            return InternalRead();
        }
    }
}
