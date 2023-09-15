global using static StaticUtils;

public static class StaticUtils
{
    public static IEnumerable<BsonDocument> GetData(Range range, int lorem = 5, int loremEnd = -1)
    {
        var allocated = 0L;

        Console.Write($"> {(range.End.Value - range.Start.Value + 1):n0} docs allocated in memory".PadRight(40, ' ') + ": ");

        for (var i = range.Start.Value; i <= range.End.Value; i++)
        {
            var doc = new BsonDocument
            {
                ["_id"] = i,
                ["name"] = Faker.Fullname(),
                ["age"] = Faker.Age(),
                ["lorem"] = Faker.Lorem(lorem, loremEnd)
            };

            allocated += doc.GetBytesCount();

            yield return doc;
        }

        Console.WriteLine($"{(allocated / 1024 / 1024):n0} MB");

    }

    public static async Task Run(string message, Func<Task> asyncFunc)
    {
        var sw = Stopwatch.StartNew();

        Console.Write((" > " + message + "... ").PadRight(40, ' ') + ": ");

        await asyncFunc();

        Console.WriteLine($"{sw.Elapsed.TotalMilliseconds:n0} ms");

        Profiler.AddResult(message, true);

        Console.ForegroundColor = ConsoleColor.Green;
        //db.DumpState();
        Console.ForegroundColor = ConsoleColor.Gray;
    }

    public static async Task ConsumeAsync(this LiteDB.Engine.ILiteEngine db, string collection, Query query, int fetchSize, int printTop = 0)
    {
        if (printTop > 0) Console.WriteLine("...");

        var index = 1;

        var cursorID = db.Query(collection, query, null, out var plan);

        var result = await db.FetchAsync(cursorID, fetchSize);

        if (printTop > 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(plan);
            Console.ForegroundColor = ConsoleColor.Gray;

            foreach (var item in result.Results)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write($"[{(index++):000}] ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(item.ToString().MaxLength(80) + $" ({item.GetBytesCount():n0} bytes)");
                Console.ForegroundColor = ConsoleColor.Gray;
                printTop--;
                if (printTop == 0) break;
            }
        }

        var total = result.FetchCount;

        while (result.HasMore)
        {
            result = await db.FetchAsync(cursorID, fetchSize);

            if (printTop > 0)
            {
                foreach (var item in result.Results)
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.Write($"[{(index++):000}] ");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(item.ToString().MaxLength(80) + $" ({item.GetBytesCount():n0} bytes)");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    printTop--;
                    if (printTop == 0) break;
                }
            }

            total += result.FetchCount;
        }

        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.Write($"[{total:n0}] ");
        Console.ForegroundColor = ConsoleColor.Gray;
    }

    public static async Task<T> RunAsync<T>(string message, Func<Task<T>> asyncFunc)
    {
        var sw = Stopwatch.StartNew();

        Console.Write((" > " + message + "... ").PadRight(40, ' ') + ": ");

        var result = await asyncFunc();

        Console.WriteLine($"{sw.Elapsed.TotalMilliseconds:n0}ms");

        return result;
    }

    public static T RunSync<T>(string message, Func<T> syncFunc)
    {
        var sw = Stopwatch.StartNew();

        Console.Write((" > " + message + "... ").PadRight(40, ' ') + ": ");

        var result = syncFunc();

        Console.WriteLine($"{sw.Elapsed.TotalMilliseconds:n0}ms");

        return result;
    }

    public static string MaxLength(this string self, int size)
    {
        return self.Length <= size ? self : self.Substring(0, size - 3) + $"...";
    }
}