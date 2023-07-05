namespace LiteDB.Tests.Internals.Engine;

public class Crc8_Tests
{
    [Fact]
    public void Crc8_Crc8_Theory()
    {
        // Arrange
        var sut = new CheckpointActions();

        // datapages ends here
        var lastPageID = 10;

        // adding log pages at position
        var logPages = new List<PageHeader>
        {
            new PageHeader { PositionID = 17, PageID = 24, TransactionID = 1, IsConfirmed = true }
        };

        // update lastPageID
        lastPageID = Math.Max(lastPageID, logPages.Max(x => x.PageID));

        // get start temp positionID and confirm transactions
        var startTempPositionID = Math.Max(lastPageID, logPages[^1].PositionID) + 1;
        var confirmedTransactions = new HashSet<int>(logPages.Where(x => x.IsConfirmed).Select(x => x.TransactionID));

        // define temp pages
        var tempPages = new List<PageHeader>
        {
            //new PageHeader { PositionID = 23, PageID = 24, TransactionID = 2, IsConfirmed = true } // 25
        };

        // Act
        var actions = sut.GetActions(
            logPages,
            confirmedTransactions,
            lastPageID,
            startTempPositionID,
            tempPages).ToArray();


        // Asserts
        actions.Length.Should().Be(5);

        // action #0
        actions[0].Action.Should().Be(CheckpointActionEnum.CopyToTempFile);
        actions[0].PositionID.Should().Be(19);
        actions[0].TargetPositionID.Should().Be(24);
        actions[0].MustClear.Should().BeFalse();

    }
}