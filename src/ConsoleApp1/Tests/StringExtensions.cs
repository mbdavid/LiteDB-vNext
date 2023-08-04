namespace ConsoleApp1.Tests;

internal static class StringExtensions
{
    public static string ToBinaryString(this uint value)
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
            str[29..];
    }
}
