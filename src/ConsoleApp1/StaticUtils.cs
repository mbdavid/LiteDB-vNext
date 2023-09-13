global using static StaticUtils;

public static class StaticUtils
{
    public static IEnumerable<BsonDocument> GetData(Range range, int lorem = 5)
    {
        for (var i = range.Start.Value; i <= range.End.Value; i++)
        {
            yield return new BsonDocument
            {
                ["_id"] = i,
                ["name"] = Faker.Fullname(),
                ["age"] = Faker.Age(),
                ["lorem"] = Faker.Lorem(lorem)
            };
        }
    }

    public static async Task Run(string message, Func<Task> asyncFunc)
    {
        var sw = Stopwatch.StartNew();

        Console.Write((" > " + message + "... ").PadRight(40, ' ') + ": ");

        await asyncFunc();

        Console.WriteLine($"{sw.Elapsed.TotalMilliseconds:n0}ms");

        Profiler.AddResult(message, true);

        Console.ForegroundColor = ConsoleColor.Green;
        //db.DumpState();
        Console.ForegroundColor = ConsoleColor.Gray;
    }

    public static async Task ConsumeAsync(this LiteDB.Engine.ILiteEngine db, Guid cursorID, int fetchSize)
    {
        var result = await db.FetchAsync(cursorID, fetchSize);
        var total = result.FetchCount;

        while (result.HasMore)
        {
            result = await db.FetchAsync(cursorID, fetchSize);
            total += result.FetchCount;
        }

        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.Write($"[{total}] ");
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
}