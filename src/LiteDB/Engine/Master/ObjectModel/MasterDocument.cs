namespace LiteDB.Engine;

internal class MasterDocument 
{
    public IDictionary<string, CollectionDocument> Collections { get; }
    //public readonly object Pragmas { get; }

    public MasterDocument(Collation collation)
    {
        this.Collections = new Dictionary<string, CollectionDocument>(byte.MaxValue, StringComparer.OrdinalIgnoreCase);

        // collation vai ser usado no pragma

    }

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

    }



}

