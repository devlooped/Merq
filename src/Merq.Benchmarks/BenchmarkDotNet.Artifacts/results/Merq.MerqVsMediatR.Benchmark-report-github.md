```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22622.575)
Intel Core i9-10900T CPU 1.90GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK 9.0.100-preview.2.24074.1
  [Host]     : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX2


```
| Method        | Mean     | Error   | StdDev   | Median   | Gen0   | Allocated |
|-------------- |---------:|--------:|---------:|---------:|-------:|----------:|
| PingMerq      | 303.8 ns | 6.05 ns | 15.84 ns | 302.7 ns | 0.0172 |     184 B |
| PingMerqAsync | 294.7 ns | 5.35 ns |  5.95 ns | 295.2 ns | 0.0248 |     264 B |
| PingMediatR   | 166.8 ns | 3.15 ns |  6.99 ns | 164.2 ns | 0.0319 |     336 B |
