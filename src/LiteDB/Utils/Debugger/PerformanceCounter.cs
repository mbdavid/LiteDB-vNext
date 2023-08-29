namespace LiteDB;

internal static class PerformanceCounter
{
    private static long _start = Stopwatch.GetTimestamp();
    private static ConcurrentDictionary<string, (long elapsed, long hits)> _counters = new();
    private static StringBuilder _results = new();

    public static PerfHit PERF_COUNTER()
    {
        var st = new StackTrace();
        var frame = st.GetFrame(1);
        var method = frame?.GetMethod();

        // fix name for async calls
        var name = method?.Name == "MoveNext" ?
            method?.ReflectedType?.DeclaringType?.Name + "." + method?.ReflectedType?.Name :
            method?.DeclaringType?.Name + "." + method?.Name;

        return new PerfHit(name, _counters);
    }

    public static void Reset()
    {
        _start = Stopwatch.GetTimestamp();
        _counters.Clear();
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
            _results.AppendLine($"{ch}  {title}  ".PadRight(screen_width - name_width, ch));
        }

        _results.AppendLine($"{("TOTAL".PadRight(50, '.'))}: {total,10} - 100,000%");

        var sorted = _counters
            .ToArray()
            .OrderByDescending(x => x.Value.elapsed)
            .ToArray();

        foreach (var item in sorted)
        {
            var wait = ((double)item.Value.elapsed / (double)global) * 100;
            var elapsed = $"{TimeSpan.FromTicks(item.Value.elapsed).TotalMilliseconds:n0}ms";
            var hit = $"{item.Value.hits:n0}";
            var perc = $"{wait:n3}";

            _results.AppendLine($"{item.Key.PadRight(name_width, '.')}: {elapsed,10} - {perc,7}% = {hit,10} hits");
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
        Console.ForegroundColor = ConsoleColor.Gray;
    }

    internal struct PerfHit : IDisposable
    {
        private readonly long _start;
        private readonly string _name;
        private readonly ConcurrentDictionary<string, (long elapsed, long hits)> _counters;

        public PerfHit(string name, ConcurrentDictionary<string, (long elapsed, long hits)> counters)
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

