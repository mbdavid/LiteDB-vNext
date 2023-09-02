namespace LiteDB.Engine;

/// <summary>
/// First initial data structure at start of disk. 
/// All information data here are immutable. Only flag controls are changed (IsDisposed)
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
internal unsafe struct FileHeader2
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = FILE_HEADER_INFO_SIZE)]
    public fixed char HeaderInfo[FILE_HEADER_INFO_SIZE]; // 27
    public byte FileVersion;          // 1
    public bool Encrypted;            // 1
    public ushort Reserved;           // 2

    public fixed byte EncryptionSalt[ENCRYPTION_SALT_SIZE];  // 16

    public Guid InstanceID;           // 16
    public DateTime CreationTime;     // 8
    public CollationInfo Collation;   // 4
    public VersionInfo EngineVersion; // 4 (80 bytes here)

    public fixed byte Reserved2[FILE_HEADER_SIZE - 81];  // 19

    public FileState FileState;       // 1


    /// <summary>
    /// Create empty version of file header
    /// </summary>
    public FileHeader2()
    {
    }

    /// <summary>
    /// Create a new file header structure and write direct on buffer
    /// </summary>
    public FileHeader2(IEngineSettings settings)
    {
        //this.HeaderInfo =  FILE_HEADER_INFO.;
        this.FileVersion = FILE_VERSION;

        this.Encrypted = settings.Password is not null;
        //this.EncryptionSalt = this.Encrypted ? AesStream.NewSalt() : new byte[ENCRYPTION_SALT_SIZE];

        this.InstanceID = Guid.NewGuid();
        this.CreationTime = DateTime.UtcNow;
        //this.Collation = settings.Collation;
        //this.EngineVersion = typeof(LiteEngine).Assembly.GetName().Version;

        this.FileState = FileState.Clean;
    }

}
