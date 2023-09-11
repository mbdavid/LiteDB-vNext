using BenchmarkDotNet.Attributes;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Diagnosers;
using LiteDB.Engine;

namespace LiteDB.Benchmark;

[StructLayout(LayoutKind.Sequential)]
public class PageHeader2
{
    public int PageID;
    public int a;
    public int b;
    public int c;
    public int d;
    public int e;
    public int f;
    public int g;
}

[RankColumn]
[MemoryDiagnoser]
public class Cast
{
    int NumeroDeItens = 100;
    byte[] buffer = new byte[32] { 1, 0, 0, 0, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 };
    [Benchmark]
    public void Antigo()
    {
        for (int i = 0; i < NumeroDeItens; i++)
        {

        }
    }
    [Benchmark]
    public void Novo()
    {
        for (int i = 0; i < NumeroDeItens; i++)
        {
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            PageHeader2 theStructure = (PageHeader2)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(PageHeader2));
            handle.Free();
        }
    }
}

