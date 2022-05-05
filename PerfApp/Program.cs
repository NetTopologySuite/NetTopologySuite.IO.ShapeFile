using BenchmarkDotNet.Running;

namespace PerfApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Perf>();
        }
    }
}
