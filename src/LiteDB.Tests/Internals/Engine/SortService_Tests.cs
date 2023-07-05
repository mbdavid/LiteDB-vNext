﻿using System.Dynamic;

namespace LiteDB.Tests.Internals.Engine;

public class SortService_Tests
{

    [Fact]
    public async Task Sort_ShouldReturnSortedByName_WhenInputUnSortedData()
    {
        // Arrange
        Randomizer.Seed = new Random(420);

        var collation = Collation.Default;

        using var stream = new MemoryStream();
        using var factory = Substitute.For<IServicesFactory>();
        using var bufferFactory = new BufferFactory();

        var streamFactory = new MemoryStreamFactory(stream);
        var context = new PipeContext();
        var faker = new Faker();

        using var sut = new SortService(streamFactory, factory);

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

        // create unsorted fake data
        var source = Enumerable.Range(1, 50000)
            .Select(i => new PipeValue(
                new PageAddress(i, 0), 
                new BsonDocument
                {
                    ["name"] = faker.Name.FullName()
                }))
            .ToArray();

        using var enumerator = new MockEnumerator(source);

        // Act
        using var sorter = sut.CreateSort("name", Query.Ascending);

        // insert all data
        await sorter.InsertDataAsync(enumerator, context);

        var result = new List<SortItem>();

        // loop over result to get sorted order
        while(true)
        {
            var item = await sorter.MoveNextAsync();

            if (item.IsEmpty) break;

            result.Add(item);
        }

        // Assert
        var sorted = source
            .OrderBy(x => x.Value.AsDocument["name"].AsString)
            .Select(x => new SortItem(x.RowID, x.Value.AsDocument["name"]))
            .ToArray();

        result.Should().BeEquivalentTo(sorted);
    }
}
