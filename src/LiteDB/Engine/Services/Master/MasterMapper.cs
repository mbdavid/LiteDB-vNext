namespace LiteDB.Engine;

[AutoInterface]
internal class MasterMapper : IMasterMapper
{
    #region $master document structure

    /*
    # Master Document Structure
    {
        "collections": {
            "<col-name>": {
                "colID": 1,
                "meta": { ... },
                "indexes": {
                    "<index-name>": {
                        "slot": 0,
                        "expr": "$._id",
                        "unique": true,
                        "headPageID": 8,
                        "headIndex": 0,
                        "tailPageID": 8,
                        "tailIndex": 1,
                        "meta": { ... }
                    },
                    //...
                }
            },
            //...
        },
        "pragmas": {
            "user_version": 0,
            "limit": 0,
            "checkpoint": 1000
        }
    }
    */

    #endregion

    public BsonDocument MapToDocument(MasterDocument master)
    {
        var doc = new BsonDocument
        {
            ["collections"] = new BsonDocument(master.Collections.ToDictionary(x => x.Key, x => (BsonValue)new BsonDocument
            {
                ["colID"] = x.Value.ColID,
                ["indexes"] = new BsonDocument(x.Value.Indexes.ToDictionary(i => i.Key, i => (BsonValue)new BsonDocument
                {
                    ["slot"] = (int)i.Value.Slot,
                    ["name"] = i.Value.Name,
                    ["expr"] = i.Value.Expression.ToString(),
                    ["unique"] = i.Value.Unique,
                    ["head"] = new BsonArray(new BsonValue[] { i.Value.Head.PageID, i.Value.Head.Index }),
                    ["tail"] = new BsonArray(new BsonValue[] { i.Value.Tail.PageID, i.Value.Tail.Index }),
                }))
            })),
            ["pragmas"] = new BsonDocument
            {
                ["user_version"] = master.Pragmas.UserVersion,
                ["limit_size"] = master.Pragmas.LimitSizeID,
                ["checkpoint"] = master.Pragmas.Checkpoint,
            }
        };

        return doc;
    }

    public MasterDocument MapToMaster(BsonDocument doc)
    {
        var master = new MasterDocument
        {
            Collections = doc["collections"].AsDocument.ToDictionary(c => c.Key, c => new CollectionDocument
            {
                ColID = (byte)c.Value["colID"],
                Name = c.Key,
                Indexes = c.Value["indexes"].AsDocument.ToDictionary(i => i.Key, i => new IndexDocument
                {
                    Slot = (byte)i.Value["slot"],
                    Name = i.Key,
                    Expression = BsonExpression.Create(i.Value["expr"]),
                    Unique = i.Value["unique"],
                    Head = new(i.Value["head"][0], (byte)i.Value["head"][1]),
                    Tail = new(i.Value["tail"][0], (byte)i.Value["tail"][1])
                })
            }),
            Pragmas = new PragmaDocument
            {
                UserVersion = doc["pragmas"]["user_version"],
                LimitSizeID = doc["pragmas"]["limit_size"],
                Checkpoint = doc["pragmas"]["checkpoint"]
            }
        };

        return master;
    }
}
