namespace LiteDB.Engine;

internal struct ExtendSummaryInfo
{
    public int ExtendIndex;
    public byte EmptyPage;
    public byte IndexPage_00;
    public byte IndexPage_01;
    public byte DataPage_00;
    public byte DataPage_01;
    public byte DataPage_10;
    public byte DataPage_11;
}