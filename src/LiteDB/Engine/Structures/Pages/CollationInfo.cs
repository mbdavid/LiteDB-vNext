namespace LiteDB.Engine;

internal struct CollationInfo
{
    public int LCID;
    public CompareOptions CompareOptions;

    public CollationInfo()
    {
    }

    public CollationInfo(Collation collation)
    {
        this.LCID = collation.Culture.LCID;
        this.CompareOptions = collation.CompareOptions;
    }

    public Collation ToCollation() => new (this.LCID, this.CompareOptions);
}
