namespace LiteDB.Engine;

/// <summary>
/// First initial data structure at start of disk. 
/// All information data here are immutable. Only flag controls are changed (ChangeID, Recovery)
/// </summary>
internal class FileHeader
{
    /// <summary>
    /// Header info the validate that datafile is a LiteDB file (27 bytes)
    /// </summary>
    public const string HEADER_INFO = "** This is a LiteDB file **";

    /// <summary>
    /// Datafile specification version
    /// </summary>
    public const byte FILE_VERSION = 9;

    #region Buffer Field Positions

    public const int P_ENCRYPTED = 0; // 0-0 (1 byte)

    public const int P_HEADER_INFO = 1;  // 1-27 (27 bytes)
    public const int P_FILE_VERSION = 28; // 28-28 (1 byte)

    public const int P_ENCRYPTION_SALT = 29; // 29-44  (16 bytes)
    public const int P_INSTANCE_ID = 45; // 45-61  (16 bytes)

    public const int P_CREATION_TIME = 62; // 62-70 (8 bytes)
    public const int P_COLLATION = 71; // 71-78 (8 bytes - 2 int)
    public const int P_ENGINE_VERSION = 81; // 79-81 (3 bytes "6.1.4")

    // reserved 82-97 (15 bytes)

    public const int P_CHANGE_ID = 98; // (1 byte)
    public const int P_RECOVERY = 99; // (1 byte)

    #endregion

}
