namespace LiteDB.Engine;

/// <summary>
/// First initial data structure at start of disk. 
/// All information data here are immutable. Only flag controls are changed (ChangeID, Recovery)
/// </summary>
internal struct FileHeader
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

    public const int P_HEADER_INFO = 1;  // 1-27 (27 bytes)
    public const int P_FILE_VERSION = 28; // 28-28 (1 byte)

    public const int P_ENCRYPTED = 0; // 0-0 (1 byte)
    public const int P_ENCRYPTION_SALT = 29; // 29-44  (16 bytes)

    public const int P_INSTANCE_ID = 45; // 45-61  (16 bytes)
    public const int P_CREATION_TIME = 62; // 62-70 (8 bytes)
    public const int P_COLLATION_LCID = 71; // 71-74 (4 bytes)
    public const int P_COLLATION_OPTS = 75; // 75-78 (4 bytes)
    public const int P_ENGINE_VER_MAJOR = 81; // 79-81 (1 byte "6.*.*")
    public const int P_ENGINE_VER_MINOR = 82; // 79-81 (1 byte "*.1.*")
    public const int P_ENGINE_VER_BUILD = 83; // 79-81 (1 byte "*.*.4")

    // reserved 82-97 (15 bytes)

    public const int P_CHANGE_ID = 98; // (1 byte)
    public const int P_RECOVERY = 99; // (1 byte)

    #endregion

    private string _headerInfo;
    private byte _fileVersion;

    public bool Encrypted { get; }
    public byte[] EncryptionSalt { get; }

    public Guid InstanceID { get; }
    public DateTime CreationTime { get; }
    public Collation Collation { get; }
    public Version EngineVersion { get; }

    public byte[] Buffer { get; } = new byte[FILE_HEADER_SIZE];

    /// <summary>
    /// Read file header from a existing buffer data
    /// </summary>
    public FileHeader(Span<byte> buffer)
    {
        _headerInfo = buffer[P_HEADER_INFO..(P_HEADER_INFO + HEADER_INFO.Length)].ReadString();
        _fileVersion = buffer[P_FILE_VERSION];

        this.Encrypted = buffer[P_ENCRYPTED] == 1;
        this.EncryptionSalt = buffer[P_ENCRYPTION_SALT..(P_ENCRYPTION_SALT + ENCRYPTION_SALT_SIZE)].ToArray();

        this.InstanceID = buffer[P_INSTANCE_ID..].ReadGuid();
        this.CreationTime = buffer[P_CREATION_TIME..].ReadDateTime();

        var lcid = buffer[P_COLLATION_LCID..].ReadInt32();
        var opts = buffer[P_COLLATION_OPTS..].ReadInt32();

        this.Collation = new Collation(lcid, (CompareOptions)opts);

        var major = buffer[P_ENGINE_VER_MAJOR];
        var minor = buffer[P_ENGINE_VER_MINOR];
        var build = buffer[P_ENGINE_VER_BUILD];

        this.EngineVersion = new Version(major, minor, build);

        // copy buffer
        buffer.CopyTo(this.Buffer);
    }

    /// <summary>
    /// Create a new file header structure and write direct on buffer
    /// </summary>
    public FileHeader(IEngineSettings settings)
    {
        _headerInfo = HEADER_INFO;
        _fileVersion = FILE_VERSION;

        this.Encrypted = settings.Password is not null;
        this.EncryptionSalt = this.Encrypted ? AesStream.NewSalt() : new byte[ENCRYPTION_SALT_SIZE];

        this.InstanceID = Guid.NewGuid();
        this.CreationTime = DateTime.UtcNow;
        this.Collation = settings.Collation;
        this.EngineVersion = typeof(LiteEngine).Assembly.GetName().Version;

        // get buffer
        var buffer = this.Buffer.AsSpan();

        // write flags/data into file header buffer
        buffer[P_HEADER_INFO..].WriteString(_headerInfo);
        buffer[P_FILE_VERSION] = _fileVersion;

        buffer[P_ENCRYPTED] = this.Encrypted ? (byte)1 : (byte)0;
        buffer[P_ENCRYPTION_SALT..].WriteBytes(this.EncryptionSalt);

        buffer[P_INSTANCE_ID..].WriteGuid(this.InstanceID);
        buffer[P_CREATION_TIME..].WriteDateTime(this.CreationTime);
        buffer[P_COLLATION_LCID..].WriteInt32(this.Collation.Culture.LCID);
        buffer[P_COLLATION_OPTS..].WriteInt32((int)this.Collation.CompareOptions);
        buffer[P_ENGINE_VER_MAJOR] = (byte)this.EngineVersion.Major;
        buffer[P_ENGINE_VER_MINOR] = (byte)this.EngineVersion.Minor;
        buffer[P_ENGINE_VER_BUILD] = (byte)this.EngineVersion.Build;
    }

    public void ValidateHeader()
    {
        if (_headerInfo != HEADER_INFO)
            throw ERR_INVALID_DATABASE();

        if (_fileVersion != FILE_VERSION)
            throw ERR_INVALID_FILE_VERSION();
    }

}
