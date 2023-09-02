namespace LiteDB.Engine;

internal struct VersionInfo
{
    public byte Major;    // 1
    public byte Minor;    // 1
    public byte Build;    // 1
    public byte Revision; // 1

    public static VersionInfo Current => new ()
    {
        Major = (byte)(typeof(LiteEngine).Assembly.GetName()?.Version?.Major ?? 0),
        Minor = (byte)(typeof(LiteEngine).Assembly.GetName()?.Version?.Minor?? 0),
        Build = (byte)(typeof(LiteEngine).Assembly.GetName()?.Version?.Build ?? 0),
        Revision = (byte)(typeof(LiteEngine).Assembly.GetName()?.Version?.Revision ?? 0)
    };

    public VersionInfo()
    {
    }
}
