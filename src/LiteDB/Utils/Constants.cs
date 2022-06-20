namespace LiteDB;

/// <summary>
/// Class with all constants used in LiteDB + Debbuger HELPER
/// </summary>
internal class Constants
{
    /// <summary>
    /// The size of each page in disk - use 8192 as all major databases
    /// </summary>
    public const int PAGE_SIZE = 8192;

    /// <summary>
    /// Header page size
    /// </summary>
    public const int PAGE_HEADER_SIZE = 32;

    /// <summary>
    /// Get page content area size (8160)
    /// </summary>
    public const int PAGE_CONTENT_SIZE = PAGE_SIZE - PAGE_HEADER_SIZE;

    /// <summary>
    /// Get a full empty array with PAGE_SIZE (do not change any value - shared instance)
    /// </summary>
    public static readonly byte[] PAGE_EMPTY_BUFFER = new byte[PAGE_SIZE];

    /// <summary>
    /// Bytes used in encryption salt
    /// </summary>
    public const int ENCRYPTION_SALT_SIZE = 16;

    /// <summary>
    /// Get first DataPage from $master
    /// </summary>
    public const int MASTER_PAGE_ID = 2;

    /// <summary>
    /// Get colID for $master document
    /// </summary>
    public const byte MASTER_COL_ID = byte.MaxValue;

    /// <summary>
    /// Represent how many pages each extend will allocate in PFS
    /// </summary>
    public const int PFS_FIRST_PAGE_ID = 2;

    /// <summary>
    /// Represent how many pages each extend will allocate in PFS
    /// </summary>
    public const int PFS_EXTEND_SIZE = 8;

    /// <summary>
    /// Bytes used in each extend (8 pages)
    /// 1 byte for colID + 4 bytes for 8 pages bit wise for (pageType/freeSpace)
    /// </summary>
    public const int PFS_BYTES_PER_EXTEND = 5;

    /// <summary>
    /// Get how many extends a single PFS page support (1.632 extends)
    /// </summary>
    public const int PFS_EXTEND_COUNT = PAGE_CONTENT_SIZE / PFS_BYTES_PER_EXTEND;

    /// <summary>
    /// Get how many pages (data/index/empty) a single PFS page support (13.056 pages)
    /// </summary>
    public const int PFS_PAGES_COUNT = PFS_EXTEND_COUNT * PFS_BYTES_PER_EXTEND;

    /// <summary>
    /// Indicate how many PFS pages will jump to another PFS (starts in 2)
    /// </summary>
    public const int PFS_STEP_SIZE = PFS_PAGES_COUNT + 1;

    /// <summary>
    /// Define index name max length
    /// </summary>
    public static int INDEX_NAME_MAX_LENGTH = 32;

    /// <summary>
    /// Max level used on skip list (index).
    /// </summary>
    public const int MAX_LEVEL_LENGTH = 32;

    /// <summary>
    /// Max size of a index entry - usde for string, binary, array and documents. Need fit in 1 byte length
    /// </summary>
    public const int MAX_INDEX_KEY_LENGTH = 1023;

    /// <summary>
    /// Get max length of 1 single index node
    /// </summary>
    public const int MAX_INDEX_LENGTH = 1400;

    /// <summary>
    /// Document limit size - 2048 data pages limit (about 16Mb - same size as MongoDB)
    /// Using 2047 because first/last page can contain less than 8150 bytes.
    /// </summary>
    //public const int MAX_DOCUMENT_SIZE = 2047 * DataService.MAX_DATA_BYTES_PER_PAGE;

    /// <summary>
    /// Define how many transactions can be open simultaneously
    /// </summary>
    public const int MAX_OPEN_TRANSACTIONS = 100;

    /// <summary>
    /// Define how many pages all transaction will consume, in memory, before persist in disk. This amount are shared across all open transactions
    /// 100,000 ~= 1Gb memory
    /// </summary>
    public const int MAX_TRANSACTION_SIZE = 100_000; // 100_000 (default) - 1000 (for tests)

    /// <summary>
    /// Size, in PAGES, for each buffer array (used in MemoryStore)
    /// It's an array to increase after each extend - limited in highest value
    /// Each byte array will be created with this size * PAGE_SIZE
    /// Use minimal 12 to allocate at least 85Kb per segment (will use LOH)
    /// </summary>
    public static int[] MEMORY_SEGMENT_SIZES = new int[] { 12, 50, 100, 500, 1000 }; // 8Mb per extend

    /// <summary>
    /// Define how many documents will be keep in memory until clear cache and remove support to orderby/groupby
    /// </summary>
    public const int VIRTUAL_INDEX_MAX_CACHE = 2000;

    /// <summary>
    /// Define how many bytes each merge sort container will be created
    /// </summary>
    public const int CONTAINER_SORT_SIZE = 100 * PAGE_SIZE;
}
