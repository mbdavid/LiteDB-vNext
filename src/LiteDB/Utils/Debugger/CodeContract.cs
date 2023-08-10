namespace LiteDB;

/// <summary>
/// Static methods for test (in Debug mode) some parameters - ideal to debug database
/// </summary>
[DebuggerStepThrough]
internal static class CodeContract
{
    /// <summary>
    /// Ensure condition is true, otherwise throw exception (check contract)
    /// Works as ENSURE, but for non-expression/func tests (doesn't works with Span)
    /// </summary>
    [Conditional("DEBUG")]
    public static void DEBUG(bool condition, string message)
    {
        if (condition == false)
        {
            ShowError(message, null);
        }
    }

    /// <summary>
    /// If first test is true, ensure second condition to be true, otherwise throw exception (check contract)
    /// </summary>
    [Conditional("DEBUG")]
    public static void ENSURE(bool ifTest, Func<bool> condition, string? message = null)
    {
        if (ifTest && condition() == false)
        {
            ShowError(message, null);
        }
    }

    /// <summary>
    /// Ensure condition is true, otherwise throw exception (check contract)
    /// </summary>
    [Conditional("DEBUG")]
    public static void ENSURE(Func<bool> condition, string? message = null)
    {
        var result = condition();

        if (result == false)
        {
            ShowError(message, null);
        }
    }

    /// <summary>
    /// Ensure condition is true, otherwise throw exception (check contract)
    /// </summary>
    [Conditional("DEBUG")]
    public static void ENSURE<T>(T input, Func<T, bool> condition, string? message = null)
    {
        var result = condition(input);

        if (result == false)
        {
            ShowError(message, input);
        }
    }

    /// <summary>
    /// Build a pretty error message with debug informations. Used only for DEBUG
    /// </summary>
    private static void ShowError(string? message = null, object? input = default)
    {
        var st = new StackTrace();
        var frame = st.GetFrame(2);
        var method = frame?.GetMethod();

        // crazy way to detect name when async/sync
        var location = $"{method?.DeclaringType?.DeclaringType?.CleanName()}.{method?.DeclaringType?.CleanName()}.{method?.Name}";

        location = Regex.Replace(location, @"^\.", "");
        location = Regex.Replace(location, @"\.MoveNext", "");

        var err = new StringBuilder($"Error at '{location}'. ");

        if (message is not null)
        {
            err.Append(message + ". ");
        }

        if (input is not null)
        {
            err.Append(input.GetType().Name + " = " + input.Dump());
        }

        var msg = err.ToString().Trim();

        if (Debugger.IsAttached)
        {
            Debug.Fail(msg);
        }
        else
        {
            throw ERR_ENSURE(err.ToString());
        }
    }
}