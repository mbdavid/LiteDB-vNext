namespace LiteDB.Engine;

[AutoInterface]
internal class DataService : IDataService
{
    /// <summary>
    /// Get maximum data bytes[] that fit in 1 page = 7343 bytes
    /// </summary>
    public const int MAX_DATA_BYTES_PER_PAGE = AM_DATA_PAGE_SPACE_LARGE - 1;

    // dependency injection
    private readonly IAllocationMapService _allocationMap;
    private readonly IDataPageService _dataPage;
    private readonly IBsonWriter _writer;

    private readonly ITransaction _transaction;

    public DataService(IServicesFactory factory, ITransaction transaction)
    {
        _allocationMap = factory.GetAllocationMap();
        _dataPage = factory.GetDataPageService();
        _writer = factory.GetBsonWriter();

        _transaction = transaction;
    }

    /// <summary>
    /// Insert BsonDocument into new data pages
    /// </summary>
    public async Task<PageAddress> Insert(byte colID, BsonDocument doc)
    {
        var docLength = doc.GetBytesCount();

        //if (bytesLeft > MAX_DOCUMENT_SIZE) throw new LiteException(0, "Document size exceed {0} limit", MAX_DOCUMENT_SIZE);

        // rent an array to fit all document serialized
        var bufferDoc = ArrayPool<byte>.Shared.Rent(docLength);

        // write all document into buffer doc before copy to pages
        _writer.WriteDocument(bufferDoc, doc, out _);

        var bytesToCopy = Math.Min(docLength, MAX_DATA_BYTES_PER_PAGE);

        PageAddress firstBlock;

        // get first page
        var page = await _transaction.GetFreePageAsync(colID, PageType.Data, bytesToCopy);

        // one single page
        if (bytesToCopy <= docLength)
        {
            var dataBlock = _dataPage.InsertDataBlock(page, bufferDoc, PageAddress.Empty);

            firstBlock = dataBlock.RowID;
        }
        // multiple pages
        else
        {
            var pageCount = docLength / MAX_DATA_BYTES_PER_PAGE + (docLength % MAX_DATA_BYTES_PER_PAGE == 0 ? 0 : 1);
            var pages = new PageBuffer[pageCount];

            // copy first page to array
            pages[0] = page;

            // load all pages with full space available
            for (var i = 1; i < pages.Length; i++)
            {
                bytesToCopy = i == pages.Length - 1 ? 
                    (docLength - ((pages.Length - 1) * MAX_DATA_BYTES_PER_PAGE)) : 
                    MAX_DATA_BYTES_PER_PAGE;

                pages[i] = await _transaction.GetFreePageAsync(colID, PageType.Data, bytesToCopy);
            }

            var nextBlock = PageAddress.Empty;

            // copy document bytes in reverse page order (to set next block)
            for (var i = pages.Length - 1; i >= 0; i--)
            {
                var startIndex = i * MAX_DATA_BYTES_PER_PAGE;

                bytesToCopy = i == pages.Length - 1 ?
                    (docLength - ((pages.Length - 1) * MAX_DATA_BYTES_PER_PAGE)) :
                    MAX_DATA_BYTES_PER_PAGE;

                var dataBlock = _dataPage.InsertDataBlock(pages[i], 
                    bufferDoc.AsSpan(startIndex, bytesToCopy), 
                    nextBlock);

                nextBlock = dataBlock.RowID;
            }

            firstBlock = nextBlock;
        }

        // return array after use
        ArrayPool<byte>.Shared.Return(bufferDoc);

        return firstBlock;
    }
/*

    /// <summary>
    /// Update document using same page position as reference
    /// </summary>
    public void Update(CollectionPage col, PageAddress blockAddress, BsonDocument doc)
    {
        var bytesLeft = doc.GetBytesCount(true);

        if (bytesLeft > MAX_DOCUMENT_SIZE) throw new LiteException(0, "Document size exceed {0} limit", MAX_DOCUMENT_SIZE);

        DataBlock lastBlock = null;
        var updateAddress = blockAddress;

        IEnumerable <BufferSlice> source()
        {
            var bytesToCopy = 0;

            while (bytesLeft > 0)
            {
                // if last block contains new block sequence, continue updating
                if (updateAddress.IsEmpty == false)
                {
                    var dataPage = _snapshot.GetPage<DataPage>(updateAddress.PageID);
                    var currentBlock = dataPage.GetBlock(updateAddress.Index);

                    // try get full page size content (do not add DATA_BLOCK_FIXED_SIZE because will be added in UpdateBlock)
                    bytesToCopy = Math.Min(bytesLeft, dataPage.FreeBytes + currentBlock.Buffer.Count);

                    var updateBlock = dataPage.UpdateBlock(currentBlock, bytesToCopy);

                    _snapshot.AddOrRemoveFreeDataList(dataPage);

                    yield return updateBlock.Buffer;

                    lastBlock = updateBlock;

                    // go to next address (if exists)
                    updateAddress = updateBlock.NextBlock;
                }
                else
                {
                    bytesToCopy = Math.Min(bytesLeft, MAX_DATA_BYTES_PER_PAGE);
                    var dataPage = _snapshot.GetFreeDataPage(bytesToCopy + DataBlock.DATA_BLOCK_FIXED_SIZE);
                    var insertBlock = dataPage.InsertBlock(bytesToCopy, true);

                    if (lastBlock != null)
                    {
                        lastBlock.SetNextBlock(insertBlock.Position);
                    }

                    _snapshot.AddOrRemoveFreeDataList(dataPage);

                    yield return insertBlock.Buffer;

                    lastBlock = insertBlock;
                }

                bytesLeft -= bytesToCopy;
            }

            // old document was bigger than current, must delete extend blocks
            if (lastBlock.NextBlock.IsEmpty == false)
            {
                var nextBlockAddress = lastBlock.NextBlock;

                lastBlock.SetNextBlock(PageAddress.Empty);

                this.Delete(nextBlockAddress);
            }
        }

        // consume all source bytes to write BsonDocument direct into PageBuffer
        // must be fastest as possible
        using (var w = new BufferWriter(source()))
        {
            // already bytes count calculate at method start
            w.WriteDocument(doc, false);
            w.Consume();
        }
    }

    /// <summary>
    /// Get all buffer slices that address block contains. Need use BufferReader to read document
    /// </summary>
    public IEnumerable<BufferSlice> Read(PageAddress address)
    {
        while (address != PageAddress.Empty)
        {
            var dataPage = _snapshot.GetPage<DataPage>(address.PageID);

            var block = dataPage.GetBlock(address.Index);

            yield return block.Buffer;

            address = block.NextBlock;
        }
    }

    /// <summary>
    /// Delete all datablock that contains a document (can use multiples data blocks)
    /// </summary>
    public void Delete(PageAddress blockAddress)
    {
        // delete all document blocks
        while(blockAddress != PageAddress.Empty)
        {
            var page = _snapshot.GetPage<DataPage>(blockAddress.PageID);
            var block = page.GetBlock(blockAddress.Index);

            // delete block inside page
            page.DeleteBlock(blockAddress.Index);

            // fix page empty list (or delete page)
            _snapshot.AddOrRemoveFreeDataList(page);

            blockAddress = block.NextBlock;
        }
    }
*/
}