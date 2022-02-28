using BenchmarkDotNet.Attributes;

using LiteDB.Benchmark.BDocument;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Benchmark.Tests
{
    //[SimpleJob(RuntimeMoniker.Net472, baseline: true)]
    //[SimpleJob(RuntimeMoniker.NetCoreApp30)]
    //[SimpleJob(RuntimeMoniker.CoreRt30)]
    //[SimpleJob(RuntimeMoniker.Mono)]
    [RPlotExporter]
    [MemoryDiagnoser]
    public class BsonValueCompareMemoryTests
    {
        //[Params(1000, 10000)]
        //public int N;

        [GlobalSetup]
        public void Setup()
        {
        }

        // (init)4.74ns - 56b - 0.0067gen0
        [Benchmark]
        public int BsonValue_Int() => new BsonValue(10);

//        [Benchmark]
//        public BsonDocument BsonValue_Document() => new BsonDocument();
//        [Benchmark]
//        public BsonDocument BsonValue_DocumentWithId() => new BsonDocument { ["_id"] = 1 };

        // (init)1.72ns - 24b - 0.0029gen0
        [Benchmark]
        public int BValue_Int() => new BInt(10);

        // 88b - 0.0115gen0
        [Benchmark]
        public int XValue_Int() => new XValue(10);
    }
}
