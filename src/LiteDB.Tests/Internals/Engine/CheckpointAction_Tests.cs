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
        var confirmedTransactions = new HashSet<int>();
        var startTempPositionID = 100;
        var tempPages = new List<PageHeader>();

        logPages.Add(new PageHeader { PageID = 19, PositionID = 17, TransactionID = 1, IsConfirmed = false });
        logPages.Add(new PageHeader { PageID = 11, PositionID = 18, TransactionID = 1, IsConfirmed = false });
        logPages.Add(new PageHeader { PageID = 22, PositionID = 19, TransactionID = 1, IsConfirmed = false });
        logPages.Add(new PageHeader { PageID = 23, PositionID = 20, TransactionID = 1, IsConfirmed = true });

        // act
        var actions = sut.GetCheckpointActions(logPages, confirmedTransactions, startTempPositionID, tempPages);

        // asserts
        actions.Count.Should().Be(5);

        // action #0
        actions[0].Action.Should().Be(CheckpointActionEnum.CopyToTempFile);
        actions[0].PositionID.Should().Be(19);
        actions[0].TargetPositionID.Should().Be(24);
        actions[0].MustClear.Should().BeFalse();

    }
}