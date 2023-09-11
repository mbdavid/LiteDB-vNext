using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;


namespace LiteDB.Benchmark;

[RankColumn]
[MemoryDiagnoser]
public class Tokenizer_Benchmark
{
    int NumRuns = 100;

    [Benchmark]
    public void Tokenizer()
    {
        var tokenizer = new Tokenizer("{a: 10}");
        while (!tokenizer.EOF)
        {
            tokenizer.ReadToken();
        }
        
    }

    [Benchmark]
    public void JsonTokenizer()
    {
        var tokenizer = new JsonTokenizer(new StringReader("{a: 10}"));
        while (!tokenizer.EOF)
        {
            tokenizer.ReadToken();
        }
        
    }
}

