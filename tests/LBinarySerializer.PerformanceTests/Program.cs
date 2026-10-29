using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace LBinarySerializer.PerformanceTests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = ManualConfig.Create(DefaultConfig.Instance)
                .HideColumns(StatisticColumn.Error, StatisticColumn.StdDev);
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
                .Run(args.Length == 0 ? ["--filter", "*Small*"] : args, config);
        }
    }
}
