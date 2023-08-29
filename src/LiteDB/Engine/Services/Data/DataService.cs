using System;
using System.Diagnostics.Metrics;

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
        using var _pc = PERF_COUNTER(0, nameof(InsertDocumentAsync), nameof(DataService));

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

        // return dataBlockID - will be update in first insert
        var dataBlockID = PageAddress.Empty;

        var bytesLeft = docLength;
        var position = 0;
        var extend = false;

        while (true)
        {
            // get how many bytes must be copied in this page (should consider data block and new footer item)
            var pageFreeSpace = page.Header.FreeBytes - DataBlock.DATA_BLOCK_FIXED_SIZE - 4; // -4 for a possible new record (4 footer bytes)
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
                // get block segment
                var segment = PageSegment.GetSegment(page, dataBlock.DataBlockID.Index, out var _);

                // update NextDataBlock from last page
                lastDataBlock.Value.SetNextBlockID(lastPage.AsSpan(segment), dataBlock.DataBlockID);
            }
            else
            {
                // get first dataBlock dataBlockID
                dataBlockID = dataBlock.DataBlockID;
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

        return dataBlockID;
    }

    /// <summary>
    /// Update existing document in a single or multiple pages
    /// </summary>
    public async ValueTask UpdateDocumentAsync(PageAddress dataBlockID, BsonDocument doc)
    {
        var docLength = doc.GetBytesCount();

        //if (bytesLeft > MAX_DOCUMENT_SIZE) throw new LiteException(0, "Document size exceed {0} limit", MAX_DOCUMENT_SIZE);

        // rent an array to fit all document serialized
        using var bufferDoc = SharedBuffer.Rent(docLength);

        // write all document into buffer doc before copy to pages
        _bsonWriter.WriteDocument(bufferDoc.AsSpan(), doc, out _);

        // get current datablock (for first one)
        var page = await _transaction.GetPageAsync(dataBlockID.PageID);

        // get first page segment
        var segment = PageSegment.GetSegment(page, dataBlockID.Index, out _);

        // get first data block
        var dataBlock = new DataBlock(page.AsSpan(segment), dataBlockID);

        //TODO: SOMENTE PRIMEIRA PAGINA
        _dataPageService.UpdateDataBlock(page, dataBlockID.Index, page.AsSpan(segment), PageAddress.Empty);

        // get extend page value before page change
        var before = page.Header.ExtendPageValue;

        _dataPageService.UpdateDataBlock(page, dataBlockID.Index, page.AsSpan(segment), PageAddress.Empty);

        // checks if extend page value change and update map
        var after = page.Header.ExtendPageValue;

        if (before != after)
        {
            _transaction.UpdatePageMap(page.Header.PageID, after);
        }



    }

    /// <summary>
    /// Read a single document in a single/multiple pages
    /// </summary>
    public async ValueTask<BsonReadResult> ReadDocumentAsync(PageAddress dataBlockID, string[] fields)
    {
        using var _pc = PERF_COUNTER(1, nameof(ReadDocumentAsync), nameof(DataService));

        var page = await _transaction.GetPageAsync(dataBlockID.PageID);

        // get data block segment
        var segment = PageSegment.GetSegment(page, dataBlockID.Index, out _);

        var dataBlock = new DataBlock(page.AsSpan(segment), dataBlockID);

        if (dataBlock.NextBlockID.IsEmpty)
        {
            var result = _bsonReader.ReadDocument(page.AsSpan(segment.Location + DataBlock.P_BUFFER, dataBlock.DataLength), fields, false, out _);

            return result;
        }
        else
        {
            // get a full array to read all document chuncks
            using var docBuffer = SharedBuffer.Rent(dataBlock.DocumentLength);

            // copy first page into full buffer
            page.AsSpan(segment.Location + DataBlock.P_BUFFER, dataBlock.DataLength) // get dataBlock content area
                .CopyTo(docBuffer.AsSpan()); // and copy to docBuffer byte[]

            var position = dataBlock.DataLength;

            ENSURE(dataBlock.DocumentLength != int.MaxValue, new { dataBlock });

            while (dataBlock.NextBlockID.IsEmpty)
            {
                page = await _transaction.GetPageAsync(dataBlock.NextBlockID.PageID);

                segment = PageSegment.GetSegment(page, dataBlock.NextBlockID.Index, out var _);

                dataBlock = new DataBlock(page.AsSpan(segment), dataBlock.NextBlockID);

                //dataBlock.GetDataSpan(page).CopyTo(docBuffer.AsSpan(position));
                page.AsSpan(segment.Location + DataBlock.P_BUFFER, dataBlock.DataLength) // get dataBlock content area
                    .CopyTo(docBuffer.AsSpan()); // and copy to docBuffer byte[]

                position += dataBlock.DataLength;
            }

            var result = _bsonReader.ReadDocument(docBuffer.AsSpan(), fields, false, out _);

            return result;
        }
    }

    /// <summary>
    /// Delete a full document from a single or multiple pages
    /// </summary>
    public async ValueTask DeleteDocumentAsync(PageAddress dataBlockID)
    {
        while (true)
        {
            // get page from dataBlockID
            var page = await _transaction.GetPageAsync(dataBlockID.PageID);

            // get page segment
            var segment = PageSegment.GetSegment(page, dataBlockID.Index, out _);

            // and get dataBlock
            var dataBlock = new DataBlock(page.AsSpan(segment), dataBlockID);

            var before = page.Header.ExtendPageValue;

            // delete dataBlock
            _dataPageService.DeleteDataBlock(page, dataBlockID.Index);

            // checks if extend pageValue changes
            var after = page.Header.ExtendPageValue;

            if (before != after)
            {
                // update allocation map after change page
                _transaction.UpdatePageMap(page.Header.PageID, after);
            }

            // stop if there is not block to delete
            if (dataBlock.NextBlockID.IsEmpty) break;

            // go to next block
            dataBlockID = dataBlock.NextBlockID;
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