using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Merq.MerqVsMediatR;

// Debug in-process configuration
BenchmarkRunner.Run<Benchmark>();
