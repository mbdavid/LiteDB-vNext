using LiteDB.Engine;

namespace LiteDB;

internal static class PageDump
{
    public static string Render(PageBuffer page)
    {
        var sb = StringBuilderCache.Acquire();

        sb.AppendLine($"# PageBuffer.: {{ UniqueID = {page.UniqueID}, PositionID = {Dump.PageID(page.PositionID)}, SharedCounter = {page.ShareCounter}, IsDirty = {page.IsDirty}, Timestamp = {page.Timestamp}, IsDataFile = {page.IsDataFile}, IsLogFile = {page.IsLogFile}, IsTempFile = {page.IsTempFile}, ComputedCrc8 = {page.ComputeCrc8()} }}");
        sb.AppendLine($"# Header.....: {page.Header}");

        sb.AppendLine();

        if (page.Header.PageType == PageType.AllocationMap)
        {
            RenderAllocationMapPage(page, sb);
        }
        else if (page.Header.PageType == PageType.Data)
        {
            RenderDataPage(page, sb);
        }
        else if (page.Header.PageType == PageType.Index)
        {
            RenderIndexPage(page, sb);
        }

        sb.AppendLine();

        RenderPageDump(page, sb);

        var output = StringBuilderCache.Release(sb);

        return output;
    }

    private static void RenderAllocationMapPage(PageBuffer page, StringBuilder sb)
    {
        var allocationMapID = __AllocationMapPage.GetAllocationMapID(page.Header.PageID);

        for(var i = 0; i < AM_EXTEND_COUNT; i++)
        {
            var span = page.AsSpan(PAGE_HEADER_SIZE + (i * AM_BYTES_PER_EXTEND));

            var extendLocation = new ExtendLocation(allocationMapID, i);
            var extendValue = span.ReadExtendValue();

            var extendID = extendLocation.ExtendID.ToString().PadLeft(4, ' ');
            var colID = span[0].ToString().PadLeft(3, ' ');
            var firstPageID = Dump.PageID(extendLocation.FirstPageID);
            var ev = Dump.ExtendValue(extendValue);

            sb.AppendLine($"[{extendID}] = {colID} = [{firstPageID}] => {ev}");
        }
    }

    private static void RenderDataPage(PageBuffer page, StringBuilder sb)
    {
        var reader = new BsonReader();

        if (page.Header.HighestIndex == byte.MaxValue)
        {
            sb.AppendLine("# No items");
            return;
        }

        sb.AppendLine("# Segments...:");

        for (int i = 0; i <= page.Header.HighestIndex; i++)
        {
            var segment = PageSegment.GetSegment(page, (byte)i, out _);

            var index = i.ToString().PadRight(3, ' ');

            if (!segment.IsEmpty)
            {
                var dataBlock = new __DataBlock(page.AsSpan(segment), new PageAddress(page.Header.PageID, (byte)i));

                var result = reader.ReadDocument(page.AsSpan(segment.Location + __DataBlock.P_BUFFER, dataBlock.DataLength), Array.Empty<string>(), false, out _);

                var content = result.Value.ToString() +
                    (result.Fail ? "..." : "");

                sb.AppendLine($"[{index}] = {segment} => {dataBlock.NextBlockID} = {content}");
            }
            else
            {
                sb.AppendLine($"[{index}] = {segment}");
            }
        }
    }

    private static void RenderIndexPage(PageBuffer page, StringBuilder sb)
    {
        if (page.Header.HighestIndex == byte.MaxValue)
        {
            sb.AppendLine("# No items");
            return;
        }

        sb.AppendLine("# Segments...:");

        for (byte i = 0; i < page.Header.HighestIndex; i++)
        {
            var segment = PageSegment.GetSegment(page, i, out _);

            var index = i.ToString().PadRight(3, ' ');

            if (!segment.IsEmpty)
            {
                var indexNode = new __IndexNode(page, new PageAddress(page.Header.PageID, i));

                sb.AppendLine($"[{index}] = {segment} => {indexNode}");
            }
            else
            {
                sb.AppendLine($"[{index}] = {segment}");
            }
        }
    }

    private static void RenderPageDump(PageBuffer page, StringBuilder sb) 
    {
        sb.Append("# Page Dump..:");

        for (var i = 0; i < PAGE_SIZE; i++)
        {
            if (i % 32 == 0)
            {
                sb.AppendLine();
                sb.Append("[" + i.ToString().PadRight(4, ' ') + "] ");
            }
            if (i % 32 != 0 && i % 8 == 0) sb.Append(" ");
            if (i % 32 != 0 && i % 16 == 0) sb.Append(" ");

            sb.AppendFormat("{0:X2} ", page.Buffer.Span[i]);
        }
    }
}
