namespace LiteDB.Tests.Internals.Engine;

public class CheckpointAction_Tests
{
    // dependencies
    private readonly IDiskService _diskService = Substitute.For<IDiskService>();
    private readonly ICacheService _cacheService = Substitute.For<ICacheService>();
    private readonly IBufferFactory _bufferFactory = Substitute.For<IBufferFactory>();
    private readonly IWalIndexService  _walIndexService = Substitute.For<IWalIndexService>();
    private readonly IServicesFactory _factory = Substitute.For<IServicesFactory>();

    [Fact]
    public void Checkpoint_CopyToDatafile_Theory()
    {
        // Arrange
        var sut = new LogService(_diskService, _cacheService, _bufferFactory, _walIndexService, _factory);

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