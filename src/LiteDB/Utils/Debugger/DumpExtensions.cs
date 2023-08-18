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
            str[8..10] + '-' +
            str[10..12] + '-' +
            str[12..14] + '-' +
            str[14..16] + '-' +
            str[16..18] + '-' +
            str[18..20] + '-' +
            str[20..22] + '-' +
            str[22..24] + '-' +
            str[24..26] + '-' +
            str[26..28] + '-' +
            str[28..30] + '-' +
            str[30..] + " (" +
            label(str[8..10]) + label(str[10..12]) + label(str[12..14]) + label(str[14..16]) +
            label(str[16..18]) + label(str[18..20]) + label(str[20..22]) + label(str[22..24]) +
            label(str[24..26]) + label(str[26..28]) + label(str[28..30]) + label(str[30..]) + ")";

        static string label(string str)
        {
            return str switch
            {
                "00" => "e", // empty
                "01" => "d", // data page
                "10" => "i", // index page
                "11" => "f", // page full
                _ => throw new NotSupportedException()
            };
        }
    }
}
