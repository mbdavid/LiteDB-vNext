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
    /// Represent pageID of first AllocationMapPage
    /// </summary>
    public const int AMP_FIRST_PAGE_ID = 1;

    /// <summary>
    /// Represent how many pages each extend will allocate in a single AllocationMapPage
    /// </summary>
    public const int AMP_EXTEND_SIZE = 8;

    /// <summary>
    /// Bytes used in each extend (8 pages)
    /// 1 byte for colID + 4 bytes for 8 pages bit wise for (pageType/freeSpace)
    /// </summary>
    public const int AMP_BYTES_PER_EXTEND = 5;

    /// <summary>
    /// Get how many extends a single AllocationMap page support (1.632 extends)
    /// </summary>
    public const int AMP_EXTEND_COUNT = PAGE_CONTENT_SIZE / AMP_BYTES_PER_EXTEND;

    /// <summary>
    /// Get how many pages (data/index/empty) a single allocation map page support (13.056 pages)
    /// </summary>
    public const int AMP_MAP_PAGES_COUNT = AMP_EXTEND_COUNT * AMP_BYTES_PER_EXTEND;

    /// <summary>
    /// Indicate how many allocation map pages will jump to another map page (starts in 1)
    /// </summary>
    public const int AMP_STEP_SIZE = AMP_MAP_PAGES_COUNT + 1;

    /// <summary>
    /// Represent an array of how distribuited pages are inside AllocationMap 2 bits (should be 4 values only)
    /// 00 - arr[0]..8160 (page empty)
    /// 01 - (arr[0]-1)..arr[1]
    /// 10 - (arr[1]-1)..arr[2]
    /// 11 - (arr[2]-1)..0 (page full)
    /// </summary>
    public const int AMP_DATA_PAGE_SPACE_00 = 7000;
    public const int AMP_DATA_PAGE_SPACE_01 = 5000;
    public const int AMP_DATA_PAGE_SPACE_10 = 2000;
    public const int AMP_DATA_PAGE_SPACE_11 = 0;

    /// <summary>
    /// Indicate when a IndexPage must be consider full (IndexPage contains immutable-size/small nodes - don't need ranges)
    /// </summary>
    public const int AMP_INDEX_PAGE_SPACE = 1400;

    /// <summary>
    /// Get first DataPage from $master
    /// </summary>
    public const int MASTER_PAGE_ID = 2;

    /// <summary>
    /// Get colID for $master document
    /// </summary>
    public const byte MASTER_COL_ID = byte.MaxValue;

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
    /// Define how many documents will be keep in memory until clear cache and remove support to orderby/groupby
    /// </summary>
    public const int VIRTUAL_INDEX_MAX_CACHE = 2000;

    /// <summary>
    /// Define how many bytes each merge sort container will be created
    /// </summary>
    public const int CONTAINER_SORT_SIZE = 100 * PAGE_SIZE;
}
