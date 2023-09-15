namespace LiteDB.Engine;

internal class ExplainPlainBuilder
{
    private List<(string info, int deep)> _infos = new();

    public void Add(string info, int deep)
    {
        _infos.Add((info, deep));
    }

    public override string ToString()
    {
        var deeps = _infos.Max(x => x.deep);
        var lines = _infos
            .OrderByDescending(x => x.deep)
            .Select(x => "".PadRight(deeps - x.deep, ' ') + "> " + x.info);

        var output = string.Join('\n', lines);

        return output;
    }
}
