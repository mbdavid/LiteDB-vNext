namespace LiteDB.Engine;

[AutoInterface]
internal class PageService : IPageService
{
    /// <summary>
    /// Get a new page segment for this length content using fixed index
    /// </summary>
    protected PageSegment Insert(PageBuffer page, ushort bytesLength, byte index, bool isNewInsert)
    {
        ENSURE(() => index != byte.MaxValue, "Index must be 0-254");

        var span = page.AsSpan();
        ref var header = ref page.Header;

        // mark page as dirty
        page.IsDirty = true;

        //TODO: converter em um ensure
        if (!(header.FreeBytes >= bytesLength + (isNewInsert ? PageHeader.SLOT_SIZE : 0)))
        {
            throw ERR_INVALID_FREE_SPACE_PAGE(header.PageID, header.FreeBytes, bytesLength + (isNewInsert ? PageHeader.SLOT_SIZE : 0));
        }

        // calculate how many continuous bytes are available in this page
        var continuousBlocks = header.FreeBytes - header.FragmentedBytes - (isNewInsert ? PageHeader.SLOT_SIZE : 0);

        ENSURE(header, header => continuousBlocks == PAGE_SIZE - header.NextFreeLocation - header.FooterSize - (isNewInsert ? PageHeader.SLOT_SIZE : 0), "ContinuosBlock must be same as from NextFreePosition");

        // if continuous blocks are not big enough for this data, must run page defrag
        if (bytesLength > continuousBlocks)
        {
            this.Defrag(page);
        }

        if (index > header.HighestIndex || header.HighestIndex == byte.MaxValue)
        {
            ENSURE(header, header => index == (byte)(header.HighestIndex + 1), "new index must be next highest index");

            header.HighestIndex = index;
        }

        // get segment addresses
        var segmentAddr = PageSegment.GetSegmentAddr(index);

        DEBUG(span[segmentAddr.Location..].ReadUInt16() == 0, "slot position must be empty before use");
        DEBUG(span[segmentAddr.Length..].ReadUInt16() == 0, "slot length must be empty before use");

        // get next free location in page
        var location = header.NextFreeLocation;

        // write this page location in my location address
        span[segmentAddr.Location..].WriteUInt16(location);

        // write page segment length in my length address
        span[segmentAddr.Length..].WriteUInt16(bytesLength);

        // update next free location and counters
        header.ItemsCount++;
        header.UsedBytes += bytesLength;
        header.NextFreeLocation += bytesLength;

        ENSURE(header, header => location + bytesLength <= (PAGE_SIZE - (header.HighestIndex + 1) * PageHeader.SLOT_SIZE), "New buffer slice could not override footer area");

        // create page segment based new inserted segment
        return new (location, bytesLength);
    }

    /// <summary>
    /// Remove index slot about this page segment
    /// </summary>
    protected void Delete(PageBuffer page, byte index)
    {
        // get span and header instance (dirty)
        var span = page.AsSpan();
        ref var header = ref page.Header;

        // mark page as dirty
        page.IsDirty = true;

        // read block position on index slot
        var segment = PageSegment.GetSegment(page, index, out var segmentAddr);

        // clear both location/length
        span[segmentAddr.Location..].WriteUInt16(0);
        span[segmentAddr.Length..].WriteUInt16(0);

        // add as free blocks
        header.ItemsCount--;
        header.UsedBytes -= segment.Length;

        // clean block area with \0
        span.Slice(segment).Fill(0);

        // check if deleted segment are at end of page
        var isLastSegment = (segment.EndLocation == header.NextFreeLocation);

        if (isLastSegment)
        {
            // update next free location with this deleted segment
            header.NextFreeLocation = segment.Location;
        }
        else
        {
            // if segment is in middle of the page, add this blocks as fragment block
            header.FragmentedBytes += segment.Length;
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
            ENSURE(header, header => header.HighestIndex == byte.MaxValue, "if there is no items, HighestIndex must be clear");
            ENSURE(header, header => header.UsedBytes == 0, "should be no bytes used in clean page");

            header.NextFreeLocation = PAGE_HEADER_SIZE;
            header.FragmentedBytes = 0;
        }
    }

    /// <summary>
    /// Update segment bytes with new data. Current page must have bytes enougth for this new size. Index will not be changed
    /// Update will try use same segment to store. If not possible, write on end of page (with possible Defrag operation)
    /// </summary>
    protected PageSegment Update(PageBuffer page, byte index, ushort bytesLength)
    {
        ENSURE(() => bytesLength > 0, "Must update more than 0 bytes");

        // get span and header instance (dirty)
        var span = page.AsSpan();
        ref var header = ref page.Header;

        // mark page as dirty
        page.IsDirty = true;

        // read page segment
        var segment = PageSegment.GetSegment(page, index, out var segmentAddr);

        // check if current segment are at end of page
        var isLastSegment = (segment.EndLocation == header.NextFreeLocation);

        // best situation: same length
        if (bytesLength == segment.Length)
        {
            return segment;
        }
        // when new length are less than original length (will fit in current segment)
        else if (bytesLength < segment.Length)
        {
            var diff = (ushort)(segment.Length - bytesLength); // bytes removed (should > 0)

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
            span[segmentAddr.Length..].WriteUInt16(bytesLength);

            // clear fragment bytes
            span.Slice(segment.Location + bytesLength, diff).Fill(0);

            return new (segment.Location, bytesLength);
        }
        // when new length are large than current segment must remove current item and add again
        else
        {
            // clear current block
            span.Slice(segment).Fill(0);

            header.ItemsCount--;
            header.UsedBytes -= segment.Length;

            if (isLastSegment)
            {
                // if segment is end of page, must update next free location to current segment location
                header.NextFreeLocation = segment.Location;
            }
            else
            {
                // if segment is on middle of page, add content length as fragment bytes
                header.FragmentedBytes += segment.Length;
            }

            // clear slot index location/length
            span[segmentAddr.Location..].WriteUInt16(0);
            span[segmentAddr.Length..].WriteUInt16(0);

            // call insert
            return this.Insert(page, bytesLength, index, false);
        }
    }

    /// <summary>
    /// Defrag method re-organize all byte data content removing all fragmented data. This will move all page blocks
    /// to create a single continuous content area (just after header area). No index block will be changed (only positions)
    /// </summary>
    protected void Defrag(PageBuffer page)
    {
        var span = page.AsSpan();
        ref var header = ref page.Header;

        ENSURE(header, (header) => header.FragmentedBytes > 0, "do not call this when page has no fragmentation");
        ENSURE(header, (header) => header.HighestIndex < byte.MaxValue, "there is no items in this page to run defrag");

        // first get all blocks inside this page sorted by location (location, index)
        var blocks = new SortedList<ushort, byte>(header.ItemsCount);

        // use int to avoid byte overflow
        for (int index = 0; index <= header.HighestIndex; index++)
        {
            var addr = PageSegment.GetSegmentAddr((byte)index);

            var location = span[addr.Location..].ReadUInt16();

            // get only used index
            if (location != 0)
            {
                // sort by position
                blocks.Add(location, (byte)index);
            }
        }

        // here first block position
        var next = (ushort)PAGE_HEADER_SIZE;

        // now, list all segments order by location
        foreach (var slot in blocks)
        {
            var index = slot.Value;
            var location = slot.Key;

            // get segment address
            var addr = PageSegment.GetSegmentAddr(index);
            var length = span[addr.Length..].ReadUInt16();

            // if current segment are not as excpect, copy buffer to right position (excluding empty space)
            if (location != next)
            {
                ENSURE(() => location > next, "current segment position must be greater than current empty space");

                // copy from original location into new (correct) location
                var source = span[location..(location + length)];
                var dest = span[next..(next + length)];

                source.CopyTo(dest);

                // update new location for this index (on footer page)
                span[addr.Location..].WriteUInt16(next);
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
    /// * Used only in Delete() operation *
    /// </summary>
    private void UpdateHighestIndex(PageBuffer page)
    {
        // get span and header instance
        var span = page.AsSpan();
        ref var header = ref page.Header;

        //ENSURE(header.HighestIndex < byte.MaxValue, "can run only if contains a valid HighestIndex");

        // if current index is 0, clear index
        if (header.HighestIndex == 0)
        {
            header.HighestIndex = byte.MaxValue;
            return;
        }

        // start from current - 1 to 0 (should use "int" because for use ">= 0")
        for (int index = header.HighestIndex - 1; index >= 0; index--)
        {
            var segmentAddr = PageSegment.GetSegmentAddr((byte)index);
            var location = span[segmentAddr.Location..].ReadUInt16();

            if (location != 0)
            {
                header.HighestIndex = (byte)index;
                return;
            }
        }

        // there is no more slots used
        header.HighestIndex = byte.MaxValue;
    }

}
