namespace LiteDB;

internal static class Dump
{
    /// <summary>
    /// Implement a simple object deserialization do better reader in debug mode
    /// </summary>
    public static string Object(object obj)
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

    public static string PageID(int id)
    {
        return id == int.MaxValue ? "<EMPTY>" : id.ToString().PadLeft(4, '0');
    }

}
