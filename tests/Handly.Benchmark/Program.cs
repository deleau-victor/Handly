// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using BenchmarkTests.Benchmarks;

// static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new DebugInProcessConfig());

// Main(["BaseRequestBenchmark"]);
BenchmarkRunner.Run<BaseRequestBenchmark>();
