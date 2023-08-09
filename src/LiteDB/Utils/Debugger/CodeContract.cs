namespace LiteDB;

/// <summary>
/// Static methods for test (in Debug mode) some parameters - ideal to debug database
/// </summary>
internal static class CodeContract
{
    /// <summary>
    /// If first test is true, ensure second condition to be true, otherwise throw exception (check contract)
    /// </summary>
    [Conditional("DEBUG")]
    public static void ENSURE(bool ifTest, Expression<Func<bool>> condition, string? message = null)
    {
        if (ifTest) ENSURE(condition, message);
    }

    /// <summary>
    /// Ensure condition is true, otherwise throw exception (check contract)
    /// </summary>
    [Conditional("DEBUG")]
    public static void ENSURE(Expression<Func<bool>> condition, string? message = null)
    {
        var fn = condition.Compile();

        if (fn() == false)
        {
            var st = new StackTrace();
            var frame = st.GetFrame(1);
            var method = frame?.GetMethod();
            var location = $"{method?.DeclaringType?.Name}.{method?.Name}";

            var expr = condition.Body.Clean();
            var sb = new StringBuilder();

            PrintValues(condition.Body, sb);
            FindObjects(condition.Body, sb);

            var err = new StringBuilder($"ENSURE: `{expr}` is false at {location}. ");

            if (message is not null)
            {
                err.Append(message + ". ");
            }

            var msg = err.ToString() + sb.ToString();

            if (Debugger.IsAttached)
            {
                Debug.Fail(msg);
            }
            else
            {
                throw ERR_ENSURE(msg);
            }
        }
    }

    private static void FindObjects(Expression? e, StringBuilder sb)
    {
        if (e is null) return;

        if (e is BinaryExpression bin)
        {
            FindObjects(bin.Left, sb);
            FindObjects(bin.Right, sb);
        }
        if (e is MethodCallExpression call)
        {
            FindObjects(call.Object, sb);

            foreach (var arg in call.Arguments)
            {
                FindObjects(arg, sb);
            }
        }
        else if (e is ConstantExpression con)
        {
            var value = con.Value;
            var ns = value.GetType().Namespace ?? "";

            if (ns.StartsWith("LiteDB"))
            {
                if (sb.Length > 0) sb.Append(", ");

                sb.Append(value.GetType().Name + " = " + value.Dump());
            }
        }
        else if (e is MemberExpression mem)
        {
            var ns = mem.Member.DeclaringType?.Namespace ?? "";

            if (ns.StartsWith("LiteDB"))
            {
                if (sb.Length > 0) sb.Append(", ");

                var objExpr = Expression.Lambda(mem.Expression);
                var obj = objExpr.Compile().DynamicInvoke();

                sb.Append(mem.Expression.Clean() + " = " + obj.Dump());
            }

            FindObjects(mem.Expression!, sb);
        }
        else if (e is UnaryExpression un)
        {
            FindObjects(un.Operand, sb);
        }
    }

    private static void PrintValues(Expression e, StringBuilder sb)
    {
        if (e is BinaryExpression bin)
        {
            PrintValues(bin.Left, sb);
            PrintValues(bin.Right, sb);
        }
        else if (e is UnaryExpression un)
        {
            PrintValues(un.Operand, sb);
        }
        else if (e is ConstantExpression)
        {
            //
        }
        else
        {
            var result = Expression.Lambda(e).Compile().DynamicInvoke();

            if (sb.Length > 0) sb.Append(", ");

            sb.Append($"`{e.Clean()}` = {result}");
        }
    }

    private static string Clean(this Expression e)
    {
        var str = e.ToString();

        str = Regex.Replace(str, @"value\(.*?\)\.", "");
        str = Regex.Replace(str, @" AndAlso ", " && ");
        str = Regex.Replace(str, @" OrElse ", " || ");

        str = Regex.Replace(str, @"^\((.*)\)$", "$1");

        return str;
    }
}