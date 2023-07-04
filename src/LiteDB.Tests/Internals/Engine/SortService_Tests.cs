using System.Dynamic;

namespace LiteDB.Tests.Internals.Engine;

public class SortService_Tests
{

    [Fact]
    public async Task Sort_ShouldReturnSortedByName_WhenInputUnSortedData()
    {
        // Arrange
        var stream = new MemoryStream();
        var collation = Collation.Default;
        var factory = Substitute.For<IServicesFactory>();
        var streamFactory = new MemoryStreamFactory(stream);
        var bufferFactory = new BufferFactory();

        var sut = new SortService(streamFactory, factory);

        factory.CreateSortOperation(Arg.Any<BsonExpression>(), Arg.Any<int>())
            .Returns(c =>
            {
                return new SortOperation(sut, collation, factory, c.Arg<BsonExpression>(), c.Arg<int>());
            });

        factory.CreateSortContainer(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<Stream>())
            .Returns(c =>
            {
                return new SortContainer(bufferFactory, collation, c.ArgAt<int>(0), c.ArgAt<int>(1), c.Arg<Stream>());
            });


        var context = new PipeContext();

        Randomizer.Seed = new Random(420);

        var faker = new Faker();

        // create unsorted fake data
        var source = Enumerable.Range(1, 50000)
            .Select(i => new PipeValue(
                new PageAddress(i, 0), 
                new BsonDocument
                {
                    ["name"] = faker.Name.FullName()
                }))
            .ToArray();

        var enumerator = new MockEnumerator(source);

        // Act
        var sorter = sut.CreateSort("name", Query.Ascending);

        await sorter.InsertDataAsync(enumerator, context);

        var result = new List<PageAddress>();

        while(true)
        {
            var item = await sorter.MoveNextAsync();

            if (item.IsEmpty) break;

            result.Add(item);
        }

        // Assert
        var sorted = source
            .OrderBy(x => x.Value.AsDocument["name"].AsString)
            .Select(x => x.RowID)
            .ToArray();

        result.Should().BeEquivalentTo(sorted);

        //sut.Private<Queue<int>>("_left").Count.Should().Be(1);
    }
}

