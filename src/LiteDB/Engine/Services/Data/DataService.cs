namespace LiteDB.Engine;

[AutoInterface]
internal class DataService : IDataService
{
    /// <summary>
    /// Get maximum data bytes[] that fit in 1 page = 7343 bytes
    /// </summary>
    public const int MAX_DATA_BYTES_PER_PAGE = AM_DATA_PAGE_SPACE_LARGE - 1;

    // dependency injection
    private readonly IDataPageService _dataPageService;
    private readonly IBsonReader _bsonReader;
    private readonly IBsonWriter _bsonWriter;
    private readonly ITransaction _transaction;

    public DataService(
        IDataPageService dataPageService,
        IBsonReader bsonReader,
        IBsonWriter bsonWriter,
        ITransaction transaction)
    {
        _dataPageService = dataPageService;
        _bsonReader = bsonReader;
        _bsonWriter = bsonWriter;
        _transaction = transaction;
    }

    /// <summary>
    /// Insert BsonDocument into new data pages
    /// </summary>
    public async ValueTask<PageAddress> InsertDocumentAsync(byte colID, BsonDocument doc)
    {
        var docLength = doc.GetBytesCount();

        //if (bytesLeft > MAX_DOCUMENT_SIZE) throw new LiteException(0, "Document size exceed {0} limit", MAX_DOCUMENT_SIZE);

        // rent an array to fit all document serialized
        using var bufferDoc = SharedBuffer.Rent(docLength);

        // write all document into buffer doc before copy to pages
        _bsonWriter.WriteDocument(bufferDoc.AsSpan(), doc, out _);

        var bytesToCopy = Math.Min(docLength, MAX_DATA_BYTES_PER_PAGE);

        PageAddress firstBlock;

        // get first page
        var page = await _transaction.GetFreePageAsync(colID, PageType.Data, bytesToCopy);

        // one single page
        if (bytesToCopy <= docLength)
        {
            var dataBlock = _dataPageService.InsertDataBlock(page, bufferDoc.AsSpan(), PageAddress.Empty);

            firstBlock = dataBlock.RowID;

            // update allocation map after page change
            _transaction.UpdatePageMap(ref page.Header);
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

                var dataBlock = _dataPageService.InsertDataBlock(pages[i], 
                    bufferDoc.AsSpan(startIndex, bytesToCopy), 
                    nextBlock);

                nextBlock = dataBlock.RowID;

                // update allocation map after each page change
                _transaction.UpdatePageMap(ref pages[i].Header);
            }

            firstBlock = nextBlock;
        }

        return firstBlock;
    }

    /// <summary>
    /// Update existing document in a single or multiple pages
    /// </summary>
    public async ValueTask UpdateDocumentAsync(PageAddress rowID, BsonDocument doc)
    {
        var docLength = doc.GetBytesCount();

        //if (bytesLeft > MAX_DOCUMENT_SIZE) throw new LiteException(0, "Document size exceed {0} limit", MAX_DOCUMENT_SIZE);

        // rent an array to fit all document serialized
        using var bufferDoc = SharedBuffer.Rent(docLength);

        // write all document into buffer doc before copy to pages
        _bsonWriter.WriteDocument(bufferDoc.AsSpan(), doc, out _);

        // get current datablock (for first one)
        var page = await _transaction.GetPageAsync(rowID.PageID, true);
        //var dataBlock = new DataBlock(page, rowID);

        // TODO: tá implementado só pra 1 pagina
        _dataPageService.UpdateDataBlock(page, rowID.Index, bufferDoc.AsSpan(), PageAddress.Empty);

        // update allocation map after change page
        _transaction.UpdatePageMap(ref page.Header);
    }

    /// <summary>
    /// Read a single document in a single/multiple pages
    /// </summary>
    public async ValueTask<BsonReadResult> ReadDocumentAsync(PageAddress rowID, string[] fields)
    {
        var page = await _transaction.GetPageAsync(rowID.PageID, false);

        // read document size
        var segment = PageSegment.GetSegment(page, rowID.Index, out _);

        var dataBlock = new DataBlock(page, rowID);

        if (dataBlock.NextBlock.IsEmpty)
        {
            var result = _bsonReader.ReadDocument(page.AsSpan(segment.Location + DataBlock.P_BUFFER),
                fields, false, out var _);

            return result;
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Delete a full document from a single or multiple pages
    /// </summary>
    public async ValueTask DeleteDocumentAsync(PageAddress rowID)
    {
        var page = await _transaction.GetPageAsync(rowID.PageID, false);

        var dataBlock = new DataBlock(page, rowID);

        // delete first data block
        _dataPageService.DeleteDataBlock(page, rowID.Index);

        // update allocation map after change page
        _transaction.UpdatePageMap(ref page.Header);

        // keeping deleting all next pages/data blocks until nextBlock is empty
        while (!dataBlock.NextBlock.IsEmpty)
        {
            // get next page
            page = await _transaction.GetPageAsync(dataBlock.NextBlock.PageID, false);

            dataBlock = new DataBlock(page, dataBlock.NextBlock);

            // delete datablock
            _dataPageService.DeleteDataBlock(page, dataBlock.NextBlock.Index);

            // update allocation map after change page
            _transaction.UpdatePageMap(ref page.Header);
        }
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

    */
}