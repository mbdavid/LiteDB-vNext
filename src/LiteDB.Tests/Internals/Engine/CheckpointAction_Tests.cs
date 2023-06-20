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

        // datapages ends here
        var lastPageID = 10;

        var logPages = new List<PageHeader>();

        // adding log pages at position
        // logPages.Add(new PageHeader { PositionID = 17, PageID = 24, TransactionID = 1, IsConfirmed = true });
         logPages.Add(new PageHeader { PositionID = 18, PageID = 23, TransactionID = 2, IsConfirmed = false });
        //logPages.Add(new PageHeader { PositionID = 19, PageID = 24, TransactionID = 2, IsConfirmed = false });
        //logPages.Add(new PageHeader { PositionID = 20, PageID = 24, TransactionID = 2, IsConfirmed = false });
        //logPages.Add(new PageHeader { PositionID = 21, PageID = 24, TransactionID = 2, IsConfirmed = false });
        //logPages.Add(new PageHeader { PositionID = 22, PageID = 24, TransactionID = 2, IsConfirmed = false });
        logPages.Add(new PageHeader { PositionID = 23, PageID = 24, TransactionID = 2, IsConfirmed = true });

        // page 24 vazia

        // temp = 25
        var tempPages = new List<PageHeader>
        {
            new PageHeader { PositionID = 23, PageID = 24, TransactionID = 2, IsConfirmed = true } // 25
        };


        // clear 17
        // clear 19
        // clear 20
        // clear 21
        // clear 22

        // copytemp 23-25, false 
        //--
        // coptdata 18-23, true
        // copydata 25-24, false


        // CopyToDatafile 17 -> 24, true



        // update lastPageID
        lastPageID = Math.Max(lastPageID, logPages.Max(x => x.PageID));

        // get start temp positionID and confirm transactions
        var startTempPositionID = Math.Max(lastPageID, logPages[^1].PositionID) + 1;
        var confirmedTransactions = new HashSet<int>(logPages.Where(x => x.IsConfirmed).Select(x => x.TransactionID));

        // Act
        var actions = sut.GetCheckpointActions(logPages, confirmedTransactions, lastPageID, startTempPositionID, tempPages);


        foreach(var action in actions )
        {

        }

        // Asserts
        actions.Count.Should().Be(5);

        // action #0
        actions[0].Action.Should().Be(CheckpointActionEnum.CopyToTempFile);
        actions[0].PositionID.Should().Be(19);
        actions[0].TargetPositionID.Should().Be(24);
        actions[0].MustClear.Should().BeFalse();

    }
}