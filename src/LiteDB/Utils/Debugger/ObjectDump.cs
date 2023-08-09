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
            if (sb.Length > 0) sb.Append(", ");

            sb.Append($"{name} = ");

            if (value is null)
            {
                sb.Append("null");
            }
            else if (value is Array arr)
            {
                sb.Append($"[{arr.Length}]");
            }
            else
            {
                sb.Append(value.ToString());
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

        return "{ " + sb.ToString() + " }";
    }
}
