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
    public class BsonValueCompareTests
    {
        public int _nativeIntA = 1;
        public int _nativeIntB = 10;

        public BsonValue _bsonIntA = 1;
        public BsonValue _bsonIntB = 10;

        public BInt _bIntA = (BInt)1;
        public BInt _bIntB = (BInt)10;

        public XValue _xIntA = new(10);
        public XValue _xIntB = new(10);

        //[Params(1000, 10000)]
        //public int N;

        [GlobalSetup]
        public void Setup()
        {
        }

        // roda em 0.4ns
        [Benchmark]
        public int NativeInt_Int() => _nativeIntA.CompareTo(_nativeIntB);
        
        // roda de 4ns
        [Benchmark]
        public int BsonValue_Int() => _bsonIntA.CompareTo(_bsonIntB);
        
        [Benchmark]
        public int BValue_Int() => _bIntA.CompareTo(_bIntB);

        // roda 0.76ns
        [Benchmark]
        public int XValue_Int() => _xIntA.CompareTo(_xIntB);
    }
}
