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

    public static string ExtendValue(uint value)
    {
        var str = Convert.ToString(value, 2).PadLeft(32, '0');

        return str[..8] + '-' +
            str[8..11] + '-' +
            str[11..14] + '-' +
            str[14..17] + '-' +
            str[17..20] + '-' +
            str[20..23] + '-' +
            str[23..26] + '-' +
            str[26..29] + '-' +
            str[29..] + " (" +
            label(str[8..11]) + label(str[11..14]) + label(str[14..17]) + label(str[17..20]) +
            label(str[20..23]) + label(str[23..26]) + label(str[26..29]) + label(str[29..]) + ")";

        static string label(string str)
        {
            return str switch
            {
                "000" => "0", // empty
                "001" => "L", // data large
                "010" => "M", // data medium
                "011" => "S", // data small
                "100" => "i", // index 
                "101" => "F", // data FULL
                "110" => "f", // index FULL
                "111" => "*", // reserved
                _ => throw new NotSupportedException()
            };
        }
    }
}
