namespace LiteDB;

internal static class ObjectDumpExtensions
{
    /// <summary>
    /// Implement a simple object deserialization do better reader in debug mode
    /// </summary>
    public static string Dump(this object obj)
    {
        var type = obj.GetType();
        var ns = type.Namespace ?? "";

        if (!ns.StartsWith("LiteDB")) return obj.ToString() ?? "";

        var toString = Reflection.IsOverride(type.GetMethod("ToString")!);

        if (toString) return obj.ToString() ?? "";

        var sb = new StringBuilder();

        void Append(string name, object? value)
        {
            if (value is null)
            {
                sb.Append($"{name} = null");
            }
            else 
            {
                var type = value.GetType();

                if (Reflection.IsDictionary(type))
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append($"{name} = [{(value as IDictionary)!.Count}]");
                }
                else if (Reflection.IsCollection(type))
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append($"{name} = [{(value as ICollection)!.Count}]");
                }
                else if (Reflection.IsSimpleType(type))
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append($"{name} = {value}");
                }
            }
        }

        foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.GetProperty))
        {
            if (member is FieldInfo f)
            {
                if (f.Name.EndsWith("__BackingField")) continue;
                Append(f.Name, f.GetValue(obj));
            }
            else if (member is PropertyInfo p)
            {
                Append(p.Name, p.GetValue(obj));
            }
        }

        return sb.Length > 0 ? $"{{ {sb} }}" : "";
    }

    public static string PrettyName(this Type type)
    {
        var str = type.Name;

        str = Regex.Replace(str, @"^<(.*)>.*", "$1");

        return str;
    }

    /// <summary>
    /// A quick-simple expression string cleanner
    /// </summary>
    public static string Clean(this Expression e)
    {
        var str = e.ToString();

        str = Regex.Replace(str, @"value\(.*?\)\.", "");
        str = Regex.Replace(str, @"^value\(.*\.(.*)\)$", "$1");
        str = Regex.Replace(str, @" AndAlso ", " && ");
        str = Regex.Replace(str, @" OrElse ", " || ");

        str = Regex.Replace(str, @"^\((.*)\)$", "$1");

        return str;
    }
}
