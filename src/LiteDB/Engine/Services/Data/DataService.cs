namespace LiteDB.Engine;

[AutoInterface]
unsafe internal class DataService : IDataService
{
    // dependency injection
    private readonly IDataPageModifier _dataPageModifier;
    private readonly IBsonReader _bsonReader;
    private readonly IBsonWriter _bsonWriter;
    private readonly ITransaction _transaction;

    public DataService(
        IDataPageModifier dataPageModifier,
        IBsonReader bsonReader,
        IBsonWriter bsonWriter,
        ITransaction transaction)
    {
        _dataPageModifier = dataPageModifier;
        _bsonReader = bsonReader;
        _bsonWriter = bsonWriter;
        _transaction = transaction;
    }

    /// <summary>
    /// Insert BsonDocument into new data pages
    /// </summary>
    public RowID InsertDocument(byte colID, BsonDocument doc)
    {
        using var _pc = PERF_COUNTER(0, nameof(InsertDocument), nameof(DataService));

        var docLength = doc.GetBytesCount();

        //if (bytesLeft > MAX_DOCUMENT_SIZE) throw new LiteException(0, "Document size exceed {0} limit", MAX_DOCUMENT_SIZE);

        // rent an array to fit all document serialized
        using var bufferDoc = SharedBuffer.Rent(docLength);

        // write all document into buffer doc before copy to pages
        _bsonWriter.WriteDocument(bufferDoc.AsSpan(), doc, out _);

        // get first page
        var pagePtr = _transaction.GetFreeDataPage(colID);

        // keep last instance to update nextBlock
        var lastPagePtr = (PageMemory*)default;
        var lastDataBlockIndex = ushort.MaxValue;

        // return dataBlockID - will be update in first insert
        var firstDataBlockID = RowID.Empty;

        var bytesLeft = docLength;
        var position = 0;
        var extend = false;

        while (true)
        {
            // get how many bytes must be copied in this page (should consider data block and new footer item)
            var pageFreeSpace = pagePtr->FreeBytes - sizeof(DataBlock) - sizeof(PageSegment);
            var bytesToCopy = Math.Min(pageFreeSpace, bytesLeft);

            // get extend page value before page change
            var before = pagePtr->ExtendPageValue;

            var dataBlockPtr = _dataPageModifier.InsertDataBlock(pagePtr, bufferDoc.AsSpan(position, bytesToCopy), extend, out var dataBlockID);

            // checks if extend page value change and update map
            var after = pagePtr->ExtendPageValue;

            if (before != after)
            {
                _transaction.UpdatePageMap(pagePtr->PageID, after);
            }

            if (lastDataBlockIndex != ushort.MaxValue)
            {
                var lastDataBlockPtr = _dataPageModifier.GetDataBlock(lastPagePtr, lastDataBlockIndex, out _);

                // update NextDataBlock from last page
                lastDataBlockPtr->NextBlockID = dataBlockID;
            }
            else
            {
                // get first dataBlock dataBlockID
                firstDataBlockID = dataBlockID;
            }

            bytesLeft -= bytesToCopy;
            position += bytesToCopy;

            if (bytesLeft == 0) break;

            // keep last instance
            lastPagePtr = pagePtr;
            lastDataBlockIndex = dataBlockID.Index;

            pagePtr = _transaction.GetFreeDataPage(colID);

            // mark next data block as extend
            extend = true;
        }

        return firstDataBlockID;
    }

    /// <summary>
    /// Update existing document in a single or multiple pages
    /// </summary>
    public void UpdateDocument(RowID dataBlockID, BsonDocument doc)
    {
        var docLength = doc.GetBytesCount();

        //if (bytesLeft > MAX_DOCUMENT_SIZE) throw new LiteException(0, "Document size exceed {0} limit", MAX_DOCUMENT_SIZE);

        // rent an array to fit all document serialized
        using var bufferDoc = SharedBuffer.Rent(docLength);

        // write all document into buffer doc before copy to pages
        _bsonWriter.WriteDocument(bufferDoc.AsSpan(), doc, out _);

        // get current datablock (for first one)
        var pagePtr = _transaction.GetPage(dataBlockID.PageID);

//        //TODO: SOMENTE PRIMEIRA PAGINA

        // get extend page value before page change
        var before = pagePtr->ExtendPageValue;

        _dataPageModifier.UpdateDataBlock(pagePtr, dataBlockID.Index, bufferDoc.AsSpan(), RowID.Empty);

        // checks if extend page value change and update map
        var after = pagePtr->ExtendPageValue;

        if (before != after)
        {
            _transaction.UpdatePageMap(pagePtr->PageID, after);
        }
    }

    /// <summary>
    /// Read a single document in a single/multiple pages
    /// </summary>
    public BsonReadResult ReadDocument(RowID dataBlockID, string[] fields)
    {
        using var _pc = PERF_COUNTER(1, nameof(ReadDocument), nameof(DataService));

        var page = _transaction.GetPage(dataBlockID.PageID);

        // get data block segment
        var dataBlock = _dataPageModifier.GetDataBlock(page, dataBlockID.Index, out var dataBlockLength);

        if (dataBlock->NextBlockID.IsEmpty)
        {
            // get content buffer inside dataBlock 
            var span = new Span<byte>((byte*)((nint)dataBlock + sizeof(DataBlock)), dataBlockLength - sizeof(DataBlock));

            var result = _bsonReader.ReadDocument(span, fields, false, out _);

            return result;
        }
        else
        {
            throw new NotImplementedException();
//            // get a full array to read all document chuncks
//            using var docBuffer = SharedBuffer.Rent(dataBlock.DocumentLength);
//
//            // copy first page into full buffer
//            page.AsSpan(segment.Location + __DataBlock.P_BUFFER, dataBlock.DataLength) // get dataBlock content area
//                .CopyTo(docBuffer.AsSpan()); // and copy to docBuffer byte[]
//
//            var position = dataBlock.DataLength;
//
//            ENSURE(dataBlock.DocumentLength != int.MaxValue, new { dataBlock });
//
//            while (dataBlock.NextBlockID.IsEmpty)
//            {
//                page = await _transaction.GetPageAsync(dataBlock.NextBlockID.PageID);
//
//                segment = PageSegment.GetSegment(page, dataBlock.NextBlockID.Index, out var _);
//
//                dataBlock = new __DataBlock(page.AsSpan(segment), dataBlock.NextBlockID);
//
//                //dataBlock.GetDataSpan(page).CopyTo(docBuffer.AsSpan(position));
//                page.AsSpan(segment.Location + __DataBlock.P_BUFFER, dataBlock.DataLength) // get dataBlock content area
//                    .CopyTo(docBuffer.AsSpan()); // and copy to docBuffer byte[]
//
//                position += dataBlock.DataLength;
//            }
//
//            var result = _bsonReader.ReadDocument(docBuffer.AsSpan(), fields, false, out _);
//
//            return result;
        }
    }

    /// <summary>
    /// Delete a full document from a single or multiple pages
    /// </summary>
    public void DeleteDocument(RowID dataBlockID)
    {
        while (true)
        {
            // get page from dataBlockID
            var pagePtr = _transaction.GetPage(dataBlockID.PageID);

            var dataBlockPtr = _dataPageModifier.GetDataBlock(pagePtr, dataBlockID.Index, out _);

            var before = pagePtr->ExtendPageValue;

            // delete dataBlock
            _dataPageModifier.DeleteDataBlock(pagePtr, dataBlockID.Index);

            // checks if extend pageValue changes
            var after = pagePtr->ExtendPageValue;

            if (before != after)
            {
                // update allocation map after change page
                _transaction.UpdatePageMap(pagePtr->PageID, after);
            }

            // stop if there is not block to delete
            if (dataBlockPtr->NextBlockID.IsEmpty) break;

            // go to next block
            dataBlockID = dataBlockPtr->NextBlockID;
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

            __DataBlock lastBlock = null;
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
                        var dataPage = _snapshot.GetFreeDataPage(bytesToCopy + __DataBlock.DATA_BLOCK_FIXED_SIZE);
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