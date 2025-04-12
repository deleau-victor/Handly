```

BenchmarkDotNet v0.14.0, macOS Sonoma 14.1.1 (23B81) [Darwin 23.1.0]
Apple M1, 1 CPU, 8 logical and 8 physical cores
.NET SDK 9.0.100
  [Host] : .NET 9.0.0 (9.0.24.52809), Arm64 RyuJIT AdvSIMD
  .NET 9 : .NET 9.0.0 (9.0.24.52809), Arm64 RyuJIT AdvSIMD

Job=.NET 9  Runtime=.NET 9.0  

```
| Method                           | Mean      | Error    | StdDev   | Ratio    | Allocated | 
|--------------------------------- |----------:|---------:|---------:|---------:|----------:|
| MediatR_Send_WithoutBehaviors    |  77.24 ns | 0.736 ns | 0.652 ns | baseline |     288 B | 
| Handly_Dispatch_WithoutBehaviors | 103.92 ns | 1.442 ns | 1.279 ns |     +35% |     264 B | 
| MediatR_Send_WithBehaviors       | 270.31 ns | 4.843 ns | 4.531 ns |    +250% |    1072 B | 
| Handly_Dispatch_WithBehaviors    | 635.12 ns | 8.716 ns | 7.278 ns |    +722% |     864 B | 
