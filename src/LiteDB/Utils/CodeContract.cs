namespace LiteDB;

/// <summary>
/// Static methods for test (in Debug mode) some parameters - ideal to debug database
/// </summary>
internal class CodeContract
{
    /// <summary>
    /// Ensure condition is true, otherwise throw exception (check contract)
    /// </summary>
    [DebuggerHidden]
    public static void ENSURE(bool conditional, string? message = null)
    {
        if (conditional == false)
        {
            if (Debugger.IsAttached)
            {
                Debug.Fail(message);
            }
            else
            {
                throw ERR_ENSURE(message);
            }
        }
    }

    /// <summary>
    /// If ifTest are true, ensure condition is true, otherwise throw ensure exception (check contract)
    /// </summary>
    [DebuggerHidden]
    public static void ENSURE(bool ifTest, bool conditional, string? message = null)
    {
        if (ifTest && conditional == false)
        {
            if (Debugger.IsAttached)
            {
                Debug.Fail(message);
            }
            else
            {
                throw ERR_ENSURE(message);
            }
        }
    }

    /// <summary>
    /// Ensure condition is true, otherwise throw exception (check contract) [Works in DEBUG mode]
    /// </summary>
    [DebuggerHidden]
    [Conditional("DEBUG")]
    public static void DEBUG(bool conditional, string? message = null)
    {
        if (conditional == false)
        {
            if (Debugger.IsAttached)
            {
                Debug.Fail(message);
            }
            else
            {
                throw ERR_ENSURE(message);
            }
        }
    }

    /// <summary>
    /// Create a hashcode for a list of field in a class
    /// https://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-overriding-gethashcode
    /// </summary>
    public static int HASH(params object[] args)
    {
        unchecked
        {
            int hash = (int)2166136261;

            for (var i = 0; i < args.Length; i++)
            {
                hash = (hash * 16777619) ^ args[i].GetHashCode();
            }

            return hash;
        }
    }
}