using System;

namespace LiteDB.Engine;

[AutoInterface]
internal class DataService : IDataService
{
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

        // get first page
        var page = await _transaction.GetFreeDataPageAsync(colID);

        // keep last instance to update nextBlock
        PageBuffer? lastPage = null;
        DataBlock? lastDataBlock = null;

        // return rowID - will be update in first insert
        var rowID = PageAddress.Empty;

        var bytesLeft = docLength;
        var position = 0;
        var extend = false;

        while (true)
        {
            // get how many bytes must be copied in this page (should consider data block and new footer item)
            var pageFreeSpace = page.Header.FreeBytes - DataBlock.DATA_BLOCK_FIXED_SIZE - 4;
            var bytesToCopy = Math.Min(pageFreeSpace, bytesLeft);

            // get extend page value before page change
            var before = page.Header.ExtendPageValue;

            var dataBlock = _dataPageService.InsertDataBlock(page, bufferDoc.AsSpan(position, bytesToCopy), extend);

            // checks if extend page value change and update map
            var after = page.Header.ExtendPageValue;

            if (before != after)
            {
                _transaction.UpdatePageMap(page.Header.PageID, after);
            }

            if (lastPage is not null && lastDataBlock is not null)
            {
                // update NextDataBlock from last page
                lastDataBlock.Value.SetNextBlock(lastPage, dataBlock.RowID);
            }
            else
            {
                // get first dataBlock rowID
                rowID = dataBlock.RowID;
            }

            bytesLeft -= bytesToCopy;
            position += bytesToCopy;

            if (bytesLeft == 0) break;

            // keep last instance
            lastPage = page;
            lastDataBlock = dataBlock;

            page = await _transaction.GetFreeDataPageAsync(colID);

            // mark next data block as extend
            extend = true;
        }

        return rowID;
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
        var page = await _transaction.GetPageAsync(rowID.PageID);

        // get first data block
        var dataBlock = new DataBlock(page, rowID);




        // TODO: tá implementado só pra 1 pagina
        _dataPageService.UpdateDataBlock(page, rowID.Index, bufferDoc.AsSpan(), PageAddress.Empty);

        // update allocation map after change page
        _transaction.UpdatePageMap(page.Header.PageID, page.Header.ExtendPageValue);
    }

    /// <summary>
    /// Read a single document in a single/multiple pages
    /// </summary>
    public async ValueTask<BsonReadResult> ReadDocumentAsync(PageAddress rowID, string[] fields)
    {
        var page = await _transaction.GetPageAsync(rowID.PageID);

        var dataBlock = new DataBlock(page, rowID);

        if (dataBlock.NextBlock.IsEmpty)
        {
            var result = _bsonReader.ReadDocument(dataBlock.GetDataSpan(page), fields, false, out _);

            return result;
        }
        else
        {
            // get a full array to read all document chuncks
            using var docBuffer = SharedBuffer.Rent(dataBlock.DocumentLength);

            // copy first page into full buffer
            dataBlock.GetDataSpan(page).CopyTo(docBuffer.AsSpan());

            var position = dataBlock.DataLength;

            ENSURE(() => dataBlock.DocumentLength != int.MaxValue);

            while (dataBlock.NextBlock.IsEmpty)
            {
                page = await _transaction.GetPageAsync(dataBlock.NextBlock.PageID);

                dataBlock = new DataBlock(page, dataBlock.NextBlock);

                dataBlock.GetDataSpan(page).CopyTo(docBuffer.AsSpan(position));

                position += dataBlock.DataLength;
            }

            var result = _bsonReader.ReadDocument(docBuffer.AsSpan(), fields, false, out _);

            return result;
        }
    }

    /// <summary>
    /// Delete a full document from a single or multiple pages
    /// </summary>
    public async ValueTask DeleteDocumentAsync(PageAddress rowID)
    {
        while (true)
        {
            // get page from rowID
            var page = await _transaction.GetPageAsync(rowID.PageID);

            // and get dataBlock
            var dataBlock = new DataBlock(page, rowID);

            var before = page.Header.ExtendPageValue;

            // delete dataBlock
            _dataPageService.DeleteDataBlock(page, rowID.Index);

            // checks if extend pageValue changes
            var after = page.Header.ExtendPageValue;

            if (before != after)
            {
                // update allocation map after change page
                _transaction.UpdatePageMap(page.Header.PageID, after);
            }

            // stop if there is not block to delete
            if (dataBlock.NextBlock.IsEmpty) break;

            // go to next block
            rowID = dataBlock.NextBlock;
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