namespace LiteDB.Engine;

unsafe internal partial struct PageMemory
{
    public PageSegment* GetSegmentPtr(ushort index)
    {
        fixed (PageMemory* page = &this)
        {
            var segmentOffset = PAGE_SIZE - (index * sizeof(PageSegment));

            var segment = (PageSegment*)((nint)page + segmentOffset);

            return segment;
        }
    }

    public PageSegment* InsertSegment(ushort bytesLength, ushort index, bool isNewInsert)
    {
        ENSURE(index != ushort.MaxValue, new { bytesLength, index, isNewInsert });

        // mark page as dirty
        this.IsDirty = true;

        //TODO: converter em um ensure
        if (!(this.FreeBytes >= bytesLength + (isNewInsert ? sizeof(PageSegment) : 0)))
        {
            throw ERR_INVALID_FREE_SPACE_PAGE(this.PageID, this.FreeBytes, bytesLength + (isNewInsert ? sizeof(PageSegment) : 0));
        }

        // calculate how many continuous bytes are available in this page
        var continuousBlocks = this.FreeBytes - this.FragmentedBytes - (isNewInsert ? sizeof(PageSegment) : 0);

        ENSURE(continuousBlocks == PAGE_SIZE - this.NextFreeLocation - this.FooterSize - (isNewInsert ? sizeof(PageSegment) : 0), "ContinuosBlock must be same as from NextFreePosition",
            new { continuousBlocks, isNewInsert });

        // if continuous blocks are not big enough for this data, must run page defrag
        if (bytesLength > continuousBlocks)
        {
            //this.Defrag(page);
            throw new NotImplementedException();
        }

        if (index > this.HighestIndex || this.HighestIndex == ushort.MaxValue)
        {
            this.HighestIndex = index;
        }

        // get segment addresses
        var segment = this.GetSegmentPtr(index);

        ENSURE(segment->IsEmpty, "segment must be free in insert", new { location = segment->Location, length = segment->Length });

        // get next free location in page
        var location = this.NextFreeLocation;

        // update segment footer
        segment->Location = location;
        segment->Length = bytesLength;

        // update next free location and counters
        this.ItemsCount++;
        this.UsedBytes += bytesLength;
        this.NextFreeLocation += bytesLength;

        ENSURE(location + bytesLength <= (PAGE_SIZE - (this.HighestIndex + 1) * sizeof(PageSegment)), "New buffer slice could not override footer area",
            new { location, bytesLength});

        // create page segment based new inserted segment
        return segment;
    }

    /// <summary>
    /// Remove index slot about this page segment. Returns deleted page segment
    /// </summary>
    public void DeleteSegment(ushort index)
    {
        fixed (PageMemory* page = &this)
        {
            // mark page as dirty
            this.IsDirty = true;

            // read block position on index slot
            var segment = this.GetSegmentPtr(index);

            // clear both location/length
            segment->Location = 0;
            segment->Length = 0;

            // add as free blocks
            this.ItemsCount--;
            this.UsedBytes -= segment->Length;

            // clean block area with \0
            var dataPtr = (byte*)((nint)page + segment->Location); // position on page
            MarshalEx.FillZero(dataPtr, segment->Length);

            // check if deleted segment are at end of page
            var isLastSegment = (segment->EndLocation == this.NextFreeLocation);

            if (isLastSegment)
            {
                // update next free location with this deleted segment
                this.NextFreeLocation = segment->Location;
            }
            else
            {
                // if segment is in middle of the page, add this blocks as fragment block
                this.FragmentedBytes += segment->Length;
            }

            // if deleted if are HighestIndex, update HighestIndex
            if (this.HighestIndex == index)
            {
                this.UpdateHighestIndex();
            }

            // if there is no more blocks in page, clean FragmentedBytes and NextFreePosition
            if (this.ItemsCount == 0)
            {
                ENSURE(this.HighestIndex == ushort.MaxValue, "if there is no items, HighestIndex must be clear");
                ENSURE(this.UsedBytes == 0, "should be no bytes used in clean page");

                this.NextFreeLocation = PAGE_HEADER_SIZE;
                this.FragmentedBytes = 0;
            }
        }
    }

    /// <summary>
    /// </summary>
    public PageSegment* UpdateSegment(ushort index, ushort bytesLength)
    {
        fixed (PageMemory* page = &this)
        {
            ENSURE(bytesLength > 0, "Must update more than 0 bytes", new { bytesLength });

            // mark page as dirty
            this.IsDirty = true;

            // read page segment
            var segment = this.GetSegmentPtr(index);

            ENSURE(this.FreeBytes - segment->Length >= bytesLength, $"There is no free space in page {this.PageID} for {bytesLength} bytes required (free space: {this.FreeBytes}");

            // check if current segment are at end of page
            var isLastSegment = (segment->EndLocation == this.NextFreeLocation);

            // best situation: same length
            if (bytesLength == segment->Length)
            {
                return segment;
            }
            // when new length are less than original length (will fit in current segment)
            else if (bytesLength < segment->Length)
            {
                var diff = (ushort)(segment->Length - bytesLength); // bytes removed (should > 0)

                if (isLastSegment)
                {
                    // if is at end of page, must get back unused blocks 
                    this.NextFreeLocation -= diff;
                }
                else
                {
                    // is this segment are not at end, must add this as fragment
                    this.FragmentedBytes += diff;
                }

                // less blocks will be used
                this.UsedBytes -= diff;

                // update length
                segment->Length = bytesLength;

                // clear fragment bytes
                var fragment = (byte*)((nint)page + segment->Location + bytesLength);

                MarshalEx.FillZero(fragment, diff);

                return segment;
            }
            // when new length are large than current segment must remove current item and add again
            else
            {
                // clear current block
                var dataPtr = (byte*)((nint)page + segment->Location);

                MarshalEx.FillZero(dataPtr, segment->Length);

                this.ItemsCount--;
                this.UsedBytes -= segment->Length;

                if (isLastSegment)
                {
                    // if segment is end of page, must update next free location to current segment location
                    this.NextFreeLocation = segment->Location;
                }
                else
                {
                    // if segment is on middle of page, add content length as fragment bytes
                    this.FragmentedBytes += segment->Length;
                }

                // clear slot index location/length
                segment->Location = 0;
                segment->Length = 0;

                // call insert
                return InsertSegment(bytesLength, index, false);
            }
        }
    }

    /// <summary>
    /// Defrag method re-organize all byte data content removing all fragmented data. This will move all page blocks
    /// to create a single continuous content area (just after header area). No index block will be changed (only positions)
    /// </summary>
    private void Defrag()
    {
        fixed (PageMemory* page = &this)
        {
            ENSURE(this.FragmentedBytes > 0, "do not call this when page has no fragmentation");
            ENSURE(this.HighestIndex < byte.MaxValue, "there is no items in this page to run defrag");

            // first get all blocks inside this page sorted by location (location, index)
            var blocks = new SortedList<ushort, ushort>(this.ItemsCount); ;

            // get first segment
            var segment = this.GetSegmentPtr(0);

            // read all segments from footer
            for (ushort index = 0; index <= this.HighestIndex; index++)
            {
                // get only used index
                if (segment->Location != 0)
                {
                    // sort by position
                    blocks.Add(segment->Location, index);
                }

                segment--;
            }

            // here first block position
            var next = (ushort)PAGE_HEADER_SIZE;

            // now, list all segments order by location
            foreach (var slot in blocks)
            {
                var index = slot.Value;
                var location = slot.Key;

                // get segment address
                var addr = this.GetSegmentPtr(index);

                // if current segment are not as excpect, copy buffer to right position (excluding empty space)
                if (location != next)
                {
                    ENSURE(location > next, "current segment position must be greater than current empty space", new { location, next });

                    // copy from original location into new (correct) location
                    var sourcePtr = (byte*)((nint)page + location);
                    var destPtr = (byte*)((nint)page + next);

                    MarshalEx.Copy(sourcePtr, destPtr, addr->Length);

                    // update new location for this index (on footer page)
                    addr->Location = next;
                }

                next += addr->Length;
            }

            // fill all non-used content area with 0
            var endContent = PAGE_SIZE - this.FooterSize;

            var nextPtr = (byte*)((nint)page + next);
            var contentLength = endContent - next;

            MarshalEx.FillZero(nextPtr, contentLength);

            // clear fragment blocks (page are in a continuous segment)
            this.FragmentedBytes = 0;
            this.NextFreeLocation = next;
        }
    }


    /// <summary>
    /// Get a free index slot in this page
    /// </summary>
    public ushort GetFreeIndex()
    {
        // get first pointer do 0 index segment
        var segment = this.GetSegmentPtr(0);

        // check for all slot area to get first empty slot [safe for byte loop]
        for (var index = 0; index <= this.HighestIndex; index++)
        {
            if (segment->Location == 0)
            {
                return (ushort)index;
            }

            segment--;
        }

        return (byte)(this.HighestIndex + 1);
    }


    /// <summary>
    /// Update HighestIndex based on current HighestIndex (step back looking for next used slot)
    /// * Used only in Delete() operation *
    /// </summary>
    private void UpdateHighestIndex()
    {
        // if current index is 0, clear index
        if (this.HighestIndex == 0)
        {
            this.HighestIndex = byte.MaxValue;
            return;
        }

        var highestIndex = (ushort)(this.HighestIndex - 1);
        var segment = this.GetSegmentPtr(highestIndex);

        // start from current - 1 to 0 (should use "int" because for use ">= 0")
        for (int index = highestIndex; index >= 0; index--)
        {
            if (segment->Location != 0)
            {
                this.HighestIndex = (ushort)index;
                return;
            }

            segment++;
        }

        // there is no more slots used
        this.HighestIndex = ushort.MaxValue;
    }

}
