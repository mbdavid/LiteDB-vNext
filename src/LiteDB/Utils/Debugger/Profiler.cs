namespace LiteDB;

internal static class Profiler
{
    private const int COUNTERS = 200;

    private static long _start = Stopwatch.GetTimestamp();
    private static readonly Counter[] _counters = new Counter[COUNTERS];
    private static readonly StringBuilder _results = new();
    private static long _maxAllocatedBytes;

    static Profiler()
    {
        Reset();
    }

    public static IDisposable PERF_COUNTER(int index, string methodName, string typeName)
    {
        //return new PerfHit();
        var counter = _counters[index];

        var allocatedBytes = GC.GetTotalAllocatedBytes();

        _maxAllocatedBytes = Math.Max(_maxAllocatedBytes, allocatedBytes);

        if (counter is null)
        {
            counter = new Counter(typeName + "." + methodName);
            _counters[index] = counter;
        }

        return new ProfileHit(counter);
    }

    public static void Reset()
    {
        _start = Stopwatch.GetTimestamp();

        for(var i = 0; i < COUNTERS; i++)
        {
            if (_counters[i] is null) continue;

            _counters[i].Elapsed = 0;
            _counters[i].Hits = 0;
        }
    }

    public static void AddResult(string? title, bool reset)
    {
        const int name_width = 50;
        const int screen_width = 91;

        var global = Stopwatch.GetTimestamp() - _start;
        var total = $"{TimeSpan.FromTicks(global).TotalMilliseconds:n0}ms";

        if (title is not null)
        {
            const char ch = '=';
            _results.Append("".PadLeft(name_width, ch));
            _results.AppendLine($"|  {title}  |".PadRight(screen_width - name_width, ch));
        }

        _results.AppendLine($"{("> Total Time Spent".PadRight(50, '.'))}: {total,10} - 100,000%");

        var sorted = _counters
            .Where(x => x is not null)
            .Where(x => x.Hits > 0)
            .OrderByDescending(x => x.Elapsed)
            .ToArray();

        foreach (var item in sorted)
        {
            var wait = ((double)item.Elapsed / (double)global) * 100;
            var elapsed = $"{TimeSpan.FromTicks(item.Elapsed).TotalMilliseconds:n0}ms";
            var hit = $"{item.Hits:n0}";
            var perc = $"{wait:n3}";

            _results.AppendLine($"{("- " + item.Name).PadRight(name_width, '.')}: {elapsed,10} - {perc,7}% = {hit,10} hits");
        }

        if (reset) Reset();
    }

    public static void PrintResults()
    {
        if (_results.Length == 0)
        {
            AddResult(null, true);
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"# PERFORMANCE COUNTERS");
        Console.WriteLine(_results.ToString());
        Console.WriteLine($"# MEMORY MAX USAGE");
        var allocated = $"{(double)(_maxAllocatedBytes) / 1024d / 1024d:n2}";

        Console.WriteLine($"{("> Max Memory Allocated".PadRight(50, '.'))}: {allocated,10} Mb");


        Console.ForegroundColor = ConsoleColor.Gray;
    }

    internal struct ProfileHit : IDisposable
    {
        private readonly long _start;
        private Counter? _counter;

        public ProfileHit(Counter counter)
        {
            _start = Stopwatch.GetTimestamp();
            _counter = counter;
        }

        public ProfileHit()
        {
            _counter = null;
        }

        public void Dispose()
        {
            if (_counter is null) return;

            var elapsed = Stopwatch.GetTimestamp() - _start;

            _counter.Elapsed += elapsed;
            _counter.Hits++;

            _counter = null;
        }
    }

    internal class Counter
    {
        public readonly string Name;
        public long Elapsed;
        public long Hits;

        public Counter(string name)
        {
            this.Name = name;
            this.Elapsed = 0;
            this.Hits = 0;
        }
    }
}

