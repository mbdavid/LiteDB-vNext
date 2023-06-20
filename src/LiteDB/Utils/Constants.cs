namespace LiteDB;

/// <summary>
/// Class with all constants used in LiteDB + Debbuger HELPER
/// </summary>
internal class Constants
{
    /// <summary>
    /// Initial file data descriptor size (before start database - use offset in Stream)
    /// </summary>
    public const int FILE_HEADER_SIZE = 96;

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
    public static readonly Memory<byte> PAGE_EMPTY_BUFFER = new byte[PAGE_SIZE];

    /// <summary>
    /// Bytes used in encryption salt
    /// </summary>
    public const int ENCRYPTION_SALT_SIZE = 16;

    /// <summary>
    /// Represent pageID of first AllocationMapPage (#0)
    /// </summary>
    public const int AM_FIRST_PAGE_ID = 0;

    /// <summary>
    /// Represent how many pages each extend will allocate in AllocationMapPage
    /// </summary>
    public const int AM_EXTEND_SIZE = 8;

    /// <summary>
    /// Bytes used in each extend (8 pages)
    /// 1 byte for colID + 3 bytes for 8 pages bit wise for pageType/freeSpace
    /// </summary>
    public const int AM_BYTES_PER_EXTEND = 4;

    /// <summary>
    /// Get how many extends a single AllocationMap page support (2.040 extends)
    /// </summary>
    public const int AM_EXTEND_COUNT = PAGE_CONTENT_SIZE / AM_BYTES_PER_EXTEND;

    /// <summary>
    /// Get how many pages (data/index/empty) a single allocation map page support (16.320 pages)
    /// </summary>
    public const int AM_MAP_PAGES_COUNT = AM_EXTEND_COUNT * AM_EXTEND_SIZE;

    /// <summary>
    /// Indicate how many allocation map pages will jump to another map page (starts in 0)
    /// </summary>
    public const int AM_PAGE_STEP = AM_MAP_PAGES_COUNT + 1;

    /// <summary>
    /// Represent an array of how distribuited pages are inside AllocationMap using 3 bits
    /// [000] - 0 - Empty
    /// --
    /// [001] - 1 - Data  (between 91% and 99.99% free) [LARGE]
    /// [010] - 2 - Data  (between 51% and 90% free)  [MEDIUM]
    /// [011] - 3 - Data  (between 31% and 50% free)  [SMALL]
    /// [100] - 4 - Data  (between 0% and 30% free - page full)
    /// --
    /// [101] - 5 - Index (between 8160 and 1050 bytes free)
    /// [110] - 6 - Index (between 1049 and 0 bytes free)
    /// [111] - 7 - reserved
    /// </summary>
    public const int AM_DATA_PAGE_SPACE_LARGE  = (int)(PAGE_CONTENT_SIZE * 0.9); // 7344;
    public const int AM_DATA_PAGE_SPACE_MEDIUM = (int)(PAGE_CONTENT_SIZE * 0.5); // 4095;
    public const int AM_DATA_PAGE_SPACE_SMALL  = (int)(PAGE_CONTENT_SIZE * 0.3); // 2248;

    public const int AM_INDEX_PAGE_SPACE = 1050;

    /// <summary>
    /// Get first DataPage from $master
    /// </summary>
    public const int MASTER_PAGE_ID = 1;

    /// <summary>
    /// Get colID for $master document
    /// </summary>
    public const byte MASTER_COL_ID = 255;

    /// <summary>
    /// Get colID for $master document
    /// </summary>
    public static PageAddress MASTER_ROW_ID = new (MASTER_PAGE_ID, 0);

    /// <summary>
    /// Get max colID for collections to be used by user (1..LIMIT)
    /// </summary>
    public const int MASTER_COL_LIMIT = 250;

    /// <summary>
    /// Define when ShareCounter (on PageBuffer) are in cache or not
    /// </summary>
    public const int NO_CACHE = -1; 

    /// <summary>
    /// Define index name max length
    /// </summary>
    public const int INDEX_MAX_NAME_LENGTH = 32;

    /// <summary>
    /// Max level used on skip list (index).
    /// </summary>
    public const int INDEX_MAX_LEVELS = 32;

    /// <summary>
    /// Max size of a index entry - usde for string, binary, array and documents. Need fit in 1 byte length
    /// </summary>
    public const int INDEX_MAX_KEY_LENGTH = 1023;

    /// <summary>
    /// Get default checkpoint size (in pages). This value is used inside pragma
    /// </summary>
    public const int CHECKPOINT_SIZE = 1000;

    /// <summary>
    /// Max number to keep pages in cache. After this, a CleanUp() should be execute
    /// </summary>
    public const int CACHE_LIMIT = 20000;

    /// <summary>
    /// Define how many documents will be keep in memory until clear cache and remove support to orderby/groupby
    /// </summary>
    public const int VIRTUAL_INDEX_MAX_CACHE = 2000;

    /// <summary>
    /// Define how many bytes each merge sort container will be created
    /// </summary>
    public const int CONTAINER_SORT_SIZE = 100 * PAGE_SIZE;

}
