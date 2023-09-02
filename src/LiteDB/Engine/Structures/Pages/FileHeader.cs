namespace LiteDB.Engine;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
internal unsafe struct FileHeader
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = FILE_HEADER_INFO_SIZE)]
    public fixed byte HeaderInfoPtr[FILE_HEADER_INFO_SIZE]; // 27
    public byte FileVersion;          // 1
    public bool Encrypted;            // 1
    public ushort Reserved;           // 2

    public fixed byte EncryptionSaltPrt[ENCRYPTION_SALT_SIZE];  // 16

    public Guid InstanceID;           // 16
    public DateTime CreationTime;     // 8
    public CollationInfo Collation;   // 4
    public VersionInfo EngineVersion; // 4 (80 bytes here)

    public fixed byte ReservedPrt[FILE_HEADER_SIZE - 81];  // 19

    public FileState FileState;       // 1

    /// <summary>
    /// Create empty version of file header
    /// </summary>
    public FileHeader()
    {
    }

    /// <summary>
    /// Create a new file header structure and write direct on buffer
    /// </summary>
    public void Initialize(IEngineSettings settings)
    {
        fixed(byte* strPtr = this.HeaderInfoPtr)
        {
            MarshalEx.StrUtf8Copy(strPtr, FILE_HEADER_INFO);
        }

        this.FileVersion = FILE_VERSION;
        this.Encrypted = settings.Password is not null;

        fixed (byte* encPtr = this.EncryptionSaltPrt)
        {
            var encBytes = this.Encrypted ? AesStream.NewSalt() : new byte[ENCRYPTION_SALT_SIZE];

            Marshal.Copy(encBytes, 0, (nint)encPtr, ENCRYPTION_SALT_SIZE);
        }

        this.InstanceID = Guid.NewGuid();
        this.CreationTime = DateTime.UtcNow;
        this.Collation = new(settings.Collation);
        this.EngineVersion = VersionInfo.Current;

        this.FileState = FileState.Clean;
    }

    public void ValidateFileHeader()
    {
        fixed (byte* strPtr = this.HeaderInfoPtr)
        {
            var headerInfo = MarshalEx.ReadStrUtf8(strPtr, FILE_HEADER_INFO_SIZE);

            if (headerInfo != FILE_HEADER_INFO)
                throw ERR_INVALID_DATABASE();
        }

        if (this.FileVersion != FILE_VERSION)
            throw ERR_INVALID_FILE_VERSION();
    }
}
