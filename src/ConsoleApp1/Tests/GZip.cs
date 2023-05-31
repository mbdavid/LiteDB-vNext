internal class GZip
{
    public static void Run()
    {



        var w = new BsonWriter();
        var r = new BsonReader();

        var buffer = new byte[1024];

        var doc = MasterService.CreateNewMaster();

        //var doc = new BsonDocument
        //{
        //    /*["a"] = new BsonArray { 10, 20, 30 },
        //    ["b"] = true,*/
        //    ["c"] = new BsonDocument
        //    {
        //        ["x"] = true/*,
        //["y"] = DateTime.Now*/
        //    }
        //};

        w.WriteDocument(buffer, doc, out var length);


        var doc2 = r.ReadDocument(buffer, null, false, out var len2);


        Console.WriteLine(len2 == length);
        Console.WriteLine(doc == doc2!);
    }
}
