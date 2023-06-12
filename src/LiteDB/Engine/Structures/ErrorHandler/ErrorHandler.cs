namespace LiteDB.Engine.Structures.ErrorHandler;

internal static class ErrorHandler
{
    /// <summary>
    /// Handle critial LiteDB exceptions to avoid
    /// </summary>
    public static void Handle(this Exception exception, IServicesFactory factory, bool throwException)
    {
        // any .net exception is critical (except "ArgumentException")
        // or LiteException with code >= 900
        var isCritical = exception is LiteException ex ?
            ex.IsCritical : exception is not ArgumentException;

        //TODO: do log from factory

        if (isCritical)
        {
            factory.Exception = exception;
            factory.Dispose();
        }

        if (throwException)
        {
            throw exception;
        }
    }
}