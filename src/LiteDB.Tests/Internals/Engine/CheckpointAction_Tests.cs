using Moq;

namespace LiteDB.Tests.Internals.Engine;

public class CheckpointAction_Tests
{
    [Fact]
    public void Checkpoint_CopyToDatafile_Theory()
    {
        // arrange
        var diskService = new Mock<IDiskService>();
        var cacheService = new Mock<ICacheService>();
        var bufferFactory = new Mock<IBufferFactory>();
        var walIndexService = new Mock<IWalIndexService>();
        var factory = new Mock<IServicesFactory>();

        var sut = new LogService(
            diskService.Object,
            cacheService.Object,
            bufferFactory.Object,
            walIndexService.Object, 
            factory.Object);

        var logPages = new List<PageHeader>();
        var tempPages = new List<PageHeader>();

        // datapages ends here
        var lastPageID = 10;
        var pos = lastPageID + 5; // initial log position

        // adding log pages at position 17+
        logPages.Add(new PageHeader { PositionID = ++pos, PageID = 24, TransactionID = 1, IsConfirmed = true });


        // update lastPageID
        lastPageID = Math.Max(lastPageID, logPages.Max(x => x.PageID));

        var startTempPositionID = Math.Max(lastPageID, logPages[^1].PositionID) + 1;
        var confirmedTransactions = new HashSet<int>(logPages.Where(x => x.IsConfirmed).Select(x => x.TransactionID));

        // act
        var actions = sut.GetCheckpointActions(logPages, confirmedTransactions, lastPageID, startTempPositionID, tempPages);

        // asserts
        actions.Count.Should().Be(5);

        // action #0
        actions[0].Action.Should().Be(CheckpointActionEnum.CopyToTempFile);
        actions[0].PositionID.Should().Be(19);
        actions[0].TargetPositionID.Should().Be(24);
        actions[0].MustClear.Should().BeFalse();

    }
}