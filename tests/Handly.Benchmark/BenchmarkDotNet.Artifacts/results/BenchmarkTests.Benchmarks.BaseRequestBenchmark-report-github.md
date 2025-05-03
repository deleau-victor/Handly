```

BenchmarkDotNet v0.14.0, macOS Sonoma 14.1.1 (23B81) [Darwin 23.1.0]
Apple M1, 1 CPU, 8 logical and 8 physical cores
.NET SDK 9.0.100
  [Host] : .NET 9.0.0 (9.0.24.52809), Arm64 RyuJIT AdvSIMD
  .NET 9 : .NET 9.0.0 (9.0.24.52809), Arm64 RyuJIT AdvSIMD

Job=.NET 9  Runtime=.NET 9.0  

```
| Method                           | Mean      | Error     | StdDev    | Ratio    | Allocated | 
|--------------------------------- |----------:|----------:|----------:|---------:|----------:|
| MediatR_Send_WithoutBehaviors    |  79.33 ns |  1.607 ns |  1.503 ns | baseline |     288 B | 
| Handly_Dispatch_WithoutBehaviors | 114.99 ns |  1.918 ns |  1.700 ns |     +45% |     480 B | 
| MediatR_Send_WithBehaviors       | 264.23 ns |  4.354 ns |  3.860 ns |    +233% |    1072 B | 
| Handly_Dispatch_WithBehaviors    | 591.62 ns | 11.552 ns | 12.361 ns |    +646% |    1296 B | 
