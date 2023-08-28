namespace LiteDB;

internal static class Dump
{
    /// <summary>
    /// Implement a simple object deserialization do better reader in debug mode
    /// </summary>
    public static string Object(object obj)
    {
        var type = obj.GetType();
        var sb = new StringBuilder();

        var isEmpty = type.GetProperties().FirstOrDefault(x => x.Name == "IsEmpty");
        if (isEmpty is not null && (bool)isEmpty.GetValue(obj) == true) return "<EMPTY>";

        foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.GetProperty))
        {
            if (member is FieldInfo f)
            {
                if (f.Name.EndsWith("__BackingField") || f.Name.EndsWith("__Field")) continue;

                var value = GetStringValue(f.GetValue(obj));

                if (value.Length > 0)
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append($"{f.Name}: {value}");
                }
            }
            else if (member is PropertyInfo p)
            {
                var value = GetStringValue(p.GetValue(obj));

                if (p.Name == "IsEmpty") continue;

                if (value.Length > 0)
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append($"{p.Name}: {value}");
                }

            }
        }

        return sb.Length > 0 ? $"{{ {sb} }}" : "";
    }

    /// <summary>
    /// Implement a simple array deserialization do better reader in debug mode
    /// </summary>
    public static string Array(IEnumerable enumerable)
    {
        var sb = new StringBuilder();

        foreach(var item in enumerable)
        {
            var value = GetStringValue(item);

            if (value.Length > 0)
            {
                if (sb.Length >= 0) sb.Append(", ");
                sb.Append(value);
            }
        }

        return sb.Length > 0 ? $"[ {sb} ]" : "";
    }

    private static string GetStringValue(object? value)
    {
        if (value is null)
        {
            return "null";
        }
        else
        {
            var type = value.GetType();

            if (Reflection.IsCollection(type) || Reflection.IsDictionary(type))
            {
                var count = type.GetProperties().FirstOrDefault(x => x.Name == "Count").GetValue(value, null);
                return $"[{count}]";
            }
            else if (Reflection.IsSimpleType(type))
            {
                return value.ToString();
            }
            else
            {
                var toString = Reflection.IsOverride(type.GetMethods().FirstOrDefault(x => x.Name == "ToString" && x.GetParameters().Length == 0));
                var isLiteDB = type.Namespace.StartsWith("LiteDB");
                var isEmpty = type.GetProperties().FirstOrDefault(x => x.Name == "IsEmpty");

                if (isLiteDB && isEmpty is not null && (bool)isEmpty.GetValue(value) == true) return "<EMPTY>";

                if (toString && isLiteDB)
                {
                    return value.ToString();
                }
            }
        }

        return "";
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
