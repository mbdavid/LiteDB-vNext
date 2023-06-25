namespace LiteDB.Engine;

internal class QueryDef
{
    public string CollectionName { get; set; }
    public int ColID { get; set; }
    public BsonExpression Select { get; set; } = BsonExpression.Root();
    public List<BsonExpression> Includes { get; } = new List<BsonExpression>();
    public List<BsonExpression> Filters { get; } = new List<BsonExpression>();
    public BsonExpression OrderBy { get; set; } = null;
    public int Order { get; set; } = Query.Ascending;
    public int Offset { get; set; } = 0;
    public int Limit { get; set; } = int.MaxValue;
    public Collation Collation { get; set; } = Collation.Default;
    public IndexDocument IndexDocument { get; set; }
    public BsonValue IndexKey { get; set; }



    public IIndexEnumerator CreateIndexEnumerator()
    {
        throw new NotImplementedException();
    }
}