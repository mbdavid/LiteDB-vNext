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
    /// Represent how many pages each extend will allocate in AllocationMapPage
    /// </summary>
    public const int AMP_EXTEND_SIZE = 8;

    /// <summary>
    /// Bytes used in each extend (8 pages)
    /// 1 byte for colID + 3 bytes for 8 pages bit wise for pageType/freeSpace
    /// </summary>
    public const int AMP_BYTES_PER_EXTEND = 4;

    /// <summary>
    /// Get how many extends a single AllocationMap page support (2.040 extends)
    /// </summary>
    public const int AMP_EXTEND_COUNT = PAGE_CONTENT_SIZE / AMP_BYTES_PER_EXTEND;

    /// <summary>
    /// Get how many pages (data/index/empty) a single allocation map page support (16.320 pages)
    /// </summary>
    public const int AMP_MAP_PAGES_COUNT = AMP_EXTEND_COUNT * AMP_BYTES_PER_EXTEND;

    /// <summary>
    /// Indicate how many allocation map pages will jump to another map page (starts in 1)
    /// </summary>
    public const int AMP_STEP_SIZE = AMP_MAP_PAGES_COUNT + 1;

    /// <summary>
    /// Represent an array of how distribuited pages are inside AllocationMap using 3 bits
    /// [000] - Empty
    /// --
    /// [001] - Data  (exact 8160 bytes free)
    /// [010] - Data  (between 6344 and 8159 bytes free)
    /// [011] - Data  (between 2446 and 6343 bytes free)
    /// [100] - Data  (between    0 and 2446 bytes free - page full)
    /// --
    /// [101] - Index (exact 8160 bytes free)
    /// [110] - Index (between 1050 and 8159 bytes free)
    /// [111] - Index (between    0 and 1049 bytes free - page full)
    /// </summary>
    public const int AMP_DATA_PAGE_SPACE_001 = PAGE_CONTENT_SIZE; // empty data page
    public const int AMP_DATA_PAGE_SPACE_010 = 6433;
    public const int AMP_DATA_PAGE_SPACE_011 = 2447;
    public const int AMP_DATA_PAGE_SPACE_100 = 0;

    public const int AMP_INDEX_PAGE_SPACE_101 = PAGE_CONTENT_SIZE;
    public const int AMP_INDEX_PAGE_SPACE_110 = 1050;
    public const int AMP_INDEX_PAGE_SPACE_111 = 0;

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
    /// Define how many documents will be keep in memory until clear cache and remove support to orderby/groupby
    /// </summary>
    public const int VIRTUAL_INDEX_MAX_CACHE = 2000;

    /// <summary>
    /// Define how many bytes each merge sort container will be created
    /// </summary>
    public const int CONTAINER_SORT_SIZE = 100 * PAGE_SIZE;
}
