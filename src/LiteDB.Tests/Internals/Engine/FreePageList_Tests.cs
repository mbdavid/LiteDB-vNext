namespace LiteDB.Tests.Internals.Engine;

public class FreePageList_Tests
{

    [Fact]
    public void Insert_ShouldAddOnLeft_WhenInsertNewValue()
    {
        // Arrange
        var sut = new FreePageList();

        // Act
        sut.Insert(1);

        // Assert
        sut.Private<Queue<int>>("_left").Count.Should().Be(1);
    }
}