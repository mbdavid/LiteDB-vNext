namespace LiteDB.Engine;

[AutoInterface]
internal class PageService : IPageService
{
    /// <summary>
    /// Get a new page segment for this length content using fixed index
    /// </summary>
    public PageSegment Insert(PageBuffer page, ushort bytesLength, byte index, bool isNewInsert)
    {
        ENSURE(index != byte.MaxValue, "index must be 0-254");

        var span = page.AsSpan();
        ref var header = ref page.Header;

        if (!(header.FreeBytes >= bytesLength + (isNewInsert ? PageHeader.SLOT_SIZE : 0)))
        {
            throw ERR_INVALID_FREE_SPACE_PAGE(header.PageID, header.FreeBytes, bytesLength + (isNewInsert ? PageHeader.SLOT_SIZE : 0));
        }

        // calculate how many continuous bytes are available in this page
        var continuousBlocks = header.FreeBytes - header.FragmentedBytes - (isNewInsert ? PageHeader.SLOT_SIZE : 0);

        ENSURE(continuousBlocks == PAGE_SIZE - header.NextFreeLocation - header.FooterSize - (isNewInsert ? PageHeader.SLOT_SIZE : 0), "continuosBlock must be same as from NextFreePosition");

        // if continuous blocks are not big enough for this data, must run page defrag
        if (bytesLength > continuousBlocks)
        {
            this.Defrag(page);
        }

        if (index > header.HighestIndex || header.HighestIndex == byte.MaxValue)
        {
            ENSURE(index == (byte)(header.HighestIndex + 1), "new index must be next highest index");

            header.HighestIndex = index;
        }

        // get segment addresses
        var segmentAddr = PageSegment.GetSegmentAddr(index);

        ENSURE(span[segmentAddr.Location..].ReadUInt16() == 0, "slot position must be empty before use");
        ENSURE(span[segmentAddr.Length..].ReadUInt16() == 0, "slot length must be empty before use");

        // get next free location in page
        var location = header.NextFreeLocation;

        // write this page location in my location address
        span[segmentAddr.Location..2].WriteUInt16(location);

        // write page segment length in my length address
        span[segmentAddr.Length..2].WriteUInt16(bytesLength);

        // update next free location and counters
        header.ItemsCount++;
        header.UsedBytes += bytesLength;
        header.NextFreeLocation += bytesLength;

        ENSURE(location + bytesLength <= (PAGE_SIZE - (header.HighestIndex + 1) * PageHeader.SLOT_SIZE), "new buffer slice could not override footer area");

        // create page segment based new inserted segment
        return new (location, (location + bytesLength));
    }

    /// <summary>
    /// Remove index slot about this page block
    /// </summary>
    public void Delete(PageBuffer page, byte index)
    {
        // get span and header instance (dirty)
        var span = page.AsSpan();
        ref var header = ref page.Header;

        // read block position on index slot
        var segmentAddr = PageSegment.GetSegmentAddr(index);

        var position = span[segmentAddr.Location..2].ReadUInt16();
        var length = span[segmentAddr.Length..2].ReadUInt16();

        ENSURE(this.IsValidPos(header, position), "invalid segment position");
        ENSURE(this.IsValidLen(header, length), "invalid segment length");

        // clear both position/length
        span[positionAddr..].WriteUInt16(0);
        span[lengthAddr..].WriteUInt16(0);

        // add as free blocks
        header.ItemsCount--;
        header.UsedBytes -= length;

        // clean block area with \0
        span[position..(position + length)].Fill(0);

        // check if deleted segment are at end of page
        var isLastSegment = (position + length == header.NextFreeLocation);

        if (isLastSegment)
        {
            // update next free position with this deleted position
            header.NextFreeLocation = position;
        }
        else
        {
            // if segment is in middle of the page, add this blocks as fragment block
            header.FragmentedBytes += length;
        }

        // if deleted if are HighestIndex, update HighestIndex
        if (header.HighestIndex == index)
        {
            this.UpdateHighestIndex(page);
        }

        // reset start index (used in GetFreeIndex)
        header.ResetStartIndex();

        // if there is no more blocks in page, clean FragmentedBytes and NextFreePosition
        if (header.ItemsCount == 0)
        {
            ENSURE(header.HighestIndex == byte.MaxValue, "if there is no items, HighestIndex must be clear");
            ENSURE(header.UsedBytes == 0, "should be no bytes used in clean page");
            DEBUG(span[PAGE_HEADER_SIZE..PAGE_CONTENT_SIZE].IsFullZero(), "all content area must be 0");

            header.NextFreeLocation = PAGE_HEADER_SIZE;
            header.FragmentedBytes = 0;
        }
    }

    /// <summary>
    /// Update segment bytes with new data. Current page must have bytes enougth for this new size. Index will not be changed
    /// Update will try use same segment to store. If not possible, write on end of page (with possible Defrag operation)
    /// </summary>
    public PageSegment Update(PageBuffer page, byte index, ushort bytesLength)
    {
        ENSURE(bytesLength > 0, "must update more than 0 bytes");

        // get span and header instance (dirty)
        var span = page.AsSpan();
        ref var header = ref page.Header;

        // read slot address
        var positionAddr = CalcPositionAddr(index);
        var lengthAddr = CalcLengthAddr(index);

        // read segment position/length
        var position = span[positionAddr..].ReadUInt16();
        var length = span[lengthAddr..].ReadUInt16();

        ENSURE(this.IsValidPos(header, position), "invalid segment position");
        ENSURE(this.IsValidLen(header, length), "invalid segment length");

        // check if deleted segment are at end of page
        var isLastSegment = (position + length == header.NextFreeLocation);

        // best situation: same length
        if (bytesLength == length)
        {
            return span[position..(position + length)];
        }
        // when new length are less than original length (will fit in current segment)
        else if (bytesLength < length)
        {
            var diff = (ushort)(length - bytesLength); // bytes removed (should > 0)

            if (isLastSegment)
            {
                // if is at end of page, must get back unused blocks 
                header.NextFreeLocation -= diff;
            }
            else
            {
                // is this segment are not at end, must add this as fragment
                header.FragmentedBytes += diff;
            }

            // less blocks will be used
            header.UsedBytes -= diff;

            // update length
            span[lengthAddr..].WriteUInt16(bytesLength);

            // clear fragment bytes
            var clearStart = position + bytesLength;
            var clearEnd = clearStart + diff;

            span[clearStart..clearEnd].Fill(0);

            return span[position..(position + bytesLength)];
        }
        // when new length are large than current segment must remove current item and add again
        else
        {
            // clear current block
            span[position..(position + length)].Fill(0);

            header.ItemsCount--;
            header.UsedBytes -= length;

            if (isLastSegment)
            {
                // if segment is end of page, must update next free position to current segment position
                header.NextFreeLocation = position;
            }
            else
            {
                // if segment is on middle of page, add content length as fragment bytes
                header.FragmentedBytes += length;
            }

            // clear slot index position/length
            span[positionAddr..].WriteUInt16(0);
            span[lengthAddr..].WriteUInt16(0);

            // call insert
            return this.Insert(page, bytesLength, index, false);
        }
    }

    /// <summary>
    /// Defrag method re-organize all byte data content removing all fragmented data. This will move all page blocks
    /// to create a single continuous content area (just after header area). No index block will be changed (only positions)
    /// </summary>
    public void Defrag(PageBuffer page)
    {
        // get span and header instance (dirty)
        var span = page.AsSpan();
        ref var header = ref page.Header;

        ENSURE(header.FragmentedBytes > 0, "do not call this when page has no fragmentation");
        ENSURE(header.HighestIndex < byte.MaxValue, "there is no items in this page to run defrag");

        //LOG($"defrag page #{this.PageID} (fragments: {this.FragmentedBytes})", "DISK");

        // first get all blocks inside this page sorted by position (position, index)
        var blocks = new SortedList<ushort, byte>();

        // use int to avoid byte overflow
        for (int index = 0; index <= header.HighestIndex; index++)
        {
            var positionAddr = CalcPositionAddr((byte)index);
            var position = span[positionAddr..].ReadUInt16();

            // get only used index
            if (position != 0)
            {
                ENSURE(this.IsValidPos(header, position), "invalid segment position");

                // sort by position
                blocks.Add(position, (byte)index);
            }
        }

        // here first block position
        var next = (ushort)PAGE_HEADER_SIZE;

        // now, list all segments order by Position
        foreach (var slot in blocks)
        {
            var index = slot.Value;
            var position = slot.Key;

            // get segment length
            var lengthAddr = CalcLengthAddr(index);
            var length = span[lengthAddr..].ReadUInt16();

            ENSURE(this.IsValidLen(header, length), "invalid segment length");

            // if current segment are not as excpect, copy buffer to right position (excluding empty space)
            if (position != next)
            {
                ENSURE(position > next, "current segment position must be greater than current empty space");

                throw new NotImplementedException("revisar em debug");

                // copy from original position into new (correct) position
                var source = span[position..(position + length)];
                var dest = span[next..(next + length)];

                source.CopyTo(dest);

                // update index slot with this new block position
                var positionAddr = CalcPositionAddr(index);

                // update position in footer
                span[positionAddr..].WriteUInt16(next);
            }

            next += length;
        }

        // fill all non-used content area with 0
        var endContent = PAGE_SIZE - header.FooterSize;

        span[next..endContent].Fill(0);

        // clear fragment blocks (page are in a continuous segment)
        header.FragmentedBytes = 0;
        header.NextFreeLocation = next;
    }

    /// <summary>
    /// Update HighestIndex based on current HighestIndex (step back looking for next used slot)
    /// Used only in Delete() operation
    /// </summary>
    private void UpdateHighestIndex(PageBuffer page)
    {
        // get span and header instance (dirty)
        var span = page.AsSpan();
        ref var header = ref page.Header;

        ENSURE(header.HighestIndex < byte.MaxValue, "can run only if contains a valid HighestIndex");

        // if current index is 0, clear index
        if (header.HighestIndex == 0)
        {
            header.HighestIndex = byte.MaxValue;
            return;
        }

        // start from current - 1 to 0 (should use "int" because for use ">= 0")
        for (int index = header.HighestIndex - 1; index >= 0; index--)
        {
            var positionAddr = CalcPositionAddr((byte)index);
            var position = span[positionAddr..].ReadUInt16();

            if (position != 0)
            {
                ENSURE(this.IsValidPos(header, position), "invalid segment position");

                header.HighestIndex = (byte)index;
                return;
            }
        }

        // there is no more slots used
        header.HighestIndex = byte.MaxValue;
    }

}
