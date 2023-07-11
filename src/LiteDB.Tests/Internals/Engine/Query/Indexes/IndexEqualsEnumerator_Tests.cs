namespace LiteDB.Tests.Internals.Engine;

public class IndexEqualsEnumerator_Tests
{
    [Fact]
    public void MoveNextAsync()
    {
        #region Arrange
        var indexDocument = new IndexDocument()
        {
            Slot = 1,
            Name = "IndexName",
            Expr = null,
            Unique = true,
            Head = new PageAddress(0, 1),
            Tail = new PageAddress(10, 1)

        };
        var _sut = new IndexEqualsEnumerator(1, indexDocument, Collation.Default);

        PipeContext pipeContext = new PipeContext();
        var value = new ValueTask<PipeValue>();

        #endregion


        #region Act
        value = _sut.MoveNextAsync(pipeContext);
        #endregion


        #region Asserts
        value.Should().Be(1);
        #endregion
    }
}
