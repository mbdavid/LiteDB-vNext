using System.Diagnostics;
using System.IO;
using System.Xml.Linq;

namespace LiteDB;

internal static class MethodCounter
{
    private static long _start = Stopwatch.GetTimestamp();
    private static ConcurrentDictionary<string, (long elapsed, long hits)> _counters = new();

    public static MethodHit HIT(string name) => new MethodHit(name, _counters);

    public static void PrintResults()
    {
        var global = Stopwatch.GetTimestamp() - _start;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"== COUNTERS == {TimeSpan.FromTicks(global).TotalMicroseconds:n0}ms");

        foreach (var item in _counters)
        {
            var wait = ((double)item.Value.elapsed / (double)global) * 100;
            var elapsed = $"{TimeSpan.FromTicks(item.Value.elapsed).TotalMilliseconds:n0}ms";
            var hit = $"{item.Value.hits:n0}";
            var perc = $"{wait:n3}";

            Console.WriteLine($"{item.Key,-15}: {elapsed,8} - {perc,6}% = {hit,10} hits");

        }

        Console.ForegroundColor = ConsoleColor.Gray;
    }

    internal struct MethodHit : IDisposable
    {
        private readonly long _start;
        private readonly string _name;
        private readonly ConcurrentDictionary<string, (long elapsed, long hits)> _counters;

        public MethodHit(string name, ConcurrentDictionary<string, (long elapsed, long hits)> counters)
        {
            _name = name;
            _start = Stopwatch.GetTimestamp();
            _counters = counters;
        }

        public void Dispose()
        {
            var elapsed = Stopwatch.GetTimestamp() - _start;
            var hits = 1L;

            if (_counters.TryGetValue(_name, out var res))
            {
                elapsed += res.elapsed;
                hits = res.hits + 1;
            }

            _counters[_name] = new(elapsed, hits);
        }
    }
}

