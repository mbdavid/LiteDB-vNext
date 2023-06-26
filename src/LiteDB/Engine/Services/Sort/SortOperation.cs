namespace LiteDB.Engine;

[AutoInterface]
internal class SortOperation : ISortOperation
{
    private readonly BsonExpression _expression;
    private readonly int _order;
}
