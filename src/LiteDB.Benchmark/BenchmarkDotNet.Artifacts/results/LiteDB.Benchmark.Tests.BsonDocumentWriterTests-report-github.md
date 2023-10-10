``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19044.3086 (21H2)
Intel Core i7-10700K CPU 3.80GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK=7.0.203
  [Host]     : .NET 6.0.16 (6.0.1623.17311), X64 RyuJIT
  DefaultJob : .NET 6.0.16 (6.0.1623.17311), X64 RyuJIT


```
|                            Method |      Mean |    Error |   StdDev |   Gen 0 |  Gen 1 | Allocated |
|---------------------------------- |----------:|---------:|---------:|--------:|-------:|----------:|
|     BsonDocumentWriter_SingleSpan | 104.40 μs | 0.489 μs | 0.457 μs | 12.5732 | 0.2441 |    104 KB |
|             BsonWriter_SingleSpan |  11.94 μs | 0.129 μs | 0.114 μs |  1.1444 |      - |      9 KB |
| BsonDocumentWriter_SegmentedSpans |  62.85 μs | 0.474 μs | 0.443 μs |  8.0566 | 0.1221 |     66 KB |
|         BsonWriter_SegmentedSpans |  13.06 μs | 0.160 μs | 0.142 μs |  2.8839 | 0.1068 |     24 KB |
