namespace LiteDB.Engine;

internal class MasterDocument 
{
    public IDictionary<string, CollectionDocument> Collections { get; }

    public PragmaDocument Pragmas { get; }

    /// <summary>
    /// Create a new (clean) Master document for empty database
    /// </summary>
    public MasterDocument(Collation collation)
    {
        this.Collections = new Dictionary<string, CollectionDocument>(byte.MaxValue, StringComparer.OrdinalIgnoreCase);

        this.Pragmas = new PragmaDocument(collation);
    }

    /// <summary>
    /// Create new instance of master document based on BsonDocument from disk
    /// </summary>
    public MasterDocument(BsonDocument doc)
    {
        // initialize collection dict
        this.Collections = new Dictionary<string, CollectionDocument>(byte.MaxValue, StringComparer.OrdinalIgnoreCase);

        // get all collections as colID keys
        var colDocs = doc[MK_COL].AsDocument;

        foreach (var colID in colDocs.Keys)
        {
            var colDoc = colDocs[colID].AsDocument;

            var col = new CollectionDocument(Convert.ToByte(colID), colDoc);

            this.Collections[col.Name] = col;
        }

        // load pragma info from document
        this.Pragmas = new PragmaDocument(doc[MK_PRAGMA].AsDocument);
    }
}