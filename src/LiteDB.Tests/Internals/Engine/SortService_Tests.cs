namespace LiteDB.Tests.Internals.Engine;

public class SortService_Tests
{

    [Fact]
    public async Task Insert_ShouldAddOnLeft_WhenInsertNewValue()
    {
        // Arrange

        var sut = new SortService(null, null);
        var context = new PipeContext();

        var enumerator = new MockEnumerator();

        // Act
        var sorter = sut.CreateSort("name", Query.Ascending);

        await sorter.InsertDataAsync(enumerator, context);

        while(true)
        {
            var item = await sorter.MoveNextAsync();

            Console.WriteLine(item);
        }

        // Assert
        //sut.Private<Queue<int>>("_left").Count.Should().Be(1);
    }
}

public class MockEnumerator : IPipeEnumerator
{
    private Queue<PipeValue> _items = new Queue<PipeValue>(new PipeValue[]
    {
        new PipeValue(new PageAddress(1, 0), new BsonDocument { ["name"] = "Mauricio" }),
        new PipeValue(new PageAddress(2, 0), new BsonDocument { ["name"] = "Jose" }),
        new PipeValue(new PageAddress(3, 0), new BsonDocument { ["name"] = "Ana" }),
    });

    ValueTask<PipeValue> IPipeEnumerator.MoveNextAsync(PipeContext context)
    {
        if (_items.Count == 0) return ValueTask.FromResult(PipeValue.Empty);

        var item = _items.Dequeue();

        return ValueTask.FromResult(item);
    }

    public void Dispose()
    {
    }
}