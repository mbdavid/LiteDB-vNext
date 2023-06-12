namespace LiteDB.Engine;

internal class CollectionDocument
{
    public byte ColID { get; init; }
    public required string Name { get; set; } // can be changed in RenameCollection
    public Dictionary<string, IndexDocument> Indexes { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public IndexDocument PK => this.Indexes["_id"];

    public CollectionDocument()
    {
    }

    /// <summary>
    /// Clone object instance constructor
    /// </summary>
    public CollectionDocument(CollectionDocument other)
    {
        this.ColID = other.ColID;
        this.Name = other.Name;
        this.Indexes = new Dictionary<string, IndexDocument>(other.Indexes);
    }
}

