```

BenchmarkDotNet v0.13.7, Windows 10 (10.0.19044.3086/21H2/November2021Update)
Intel Core i7-10700K CPU 3.80GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 7.0.203
  [Host]     : .NET 7.0.5 (7.0.523.17405), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.5 (7.0.523.17405), X64 RyuJIT AVX2


```
|        Method |     Mean |    Error |   StdDev | Rank |   Gen0 | Allocated |
|-------------- |---------:|---------:|---------:|-----:|-------:|----------:|
|     Tokenizer | 13.59 μs | 0.249 μs | 0.233 μs |    1 | 6.5918 |  53.91 KB |
| JsonTokenizer | 15.89 μs | 0.314 μs | 0.420 μs |    2 | 4.3945 |  35.94 KB |
