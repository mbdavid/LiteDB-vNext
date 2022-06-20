using System.Collections.Generic;
using System.IO;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// The DataPage thats stores object data.
    /// </summary>
    internal class DataPage : BlockPage
    {
        /// <summary>
        /// Create new DataPage
        /// </summary>
        public DataPage(uint pageID, byte colID)
            : base(pageID, PageType.Data, colID)
        {
        }

        /// <summary>
        /// Load data page from buffer
        /// </summary>
        public DataPage(IMemoryOwner<byte> buffer)
            : base(buffer)
        {
            ENSURE(this.PageType == PageType.Data, "page type must be data page");
        }

        /// <summary>
        /// Get single DataBlock span buffer and outs DataBlock header info
        /// </summary>
        public Span<byte> GetDataBlock(byte index, bool readOnly, out DataBlock dataBlock)
        {
            var block = base.Get(index, readOnly);

            dataBlock = new DataBlock(new PageAddress(this.PageID, index), block);

            return block[DataBlock.P_BUFFER..block.Length];
        }

        /// <summary>
        /// Insert a new datablock inside this page. Copy all content into a DataBlock
        /// </summary>
        public DataBlock InsertDataBlock(Span<byte> span, bool extend)
        {
            // get required bytes this update
            var bytesLength = (ushort)(span.Length + DataBlock.DATA_BLOCK_FIXED_SIZE);

            // get block from PageBlock
            var block = base.Insert(bytesLength, out var index);

            var position = new PageAddress(this.PageID, index);

            var dataBlock = new DataBlock(position, span, extend, PageAddress.Empty);

            // copy content from span source to block right position 
            span.CopyTo(block[DataBlock.P_BUFFER..block.Length]);

            return dataBlock;
        }

        /// <summary>
        /// Update data block content with new span buffer changes
        /// </summary>
        public void UpdateDataBlock(DataBlock currentBlock, Span<byte> span)
        {
            // get required bytes this update
            var bytesLength = (ushort)(span.Length + DataBlock.DATA_BLOCK_FIXED_SIZE);

            var block = base.Update(currentBlock.Position.Index, bytesLength);

            // copy content from span source to block right position 
            span.CopyTo(block[DataBlock.P_BUFFER..block.Length]);
        }

        /// <summary>
        /// Delete single data block inside this page
        /// </summary>
        public void DeleteBlock(byte index)
        {
            base.Delete(index);
        }
    }
}