global using static LiteDB.Constants;
global using static LiteDB.BsonExpression;
global using LiteDB;
global using LiteDB.Engine;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

var doc = new BsonDocument
{
    ["_id"] = 1,
    ["first"] = "Mauricio",
    ["last"] = "david",
    ["age"] = 45,
    ["address"] = new BsonDocument
    {
        ["street"] = "av pernambuco",
        ["number"] = 123
    }
};

var parameters = new BsonDocument
{
    ["qrcode"] = "237467846234263",
};


BsonExpression expr = "_id = @qrcode";
//BsonExpression expr = BsonExpression.Add(BsonExpression.Constant(50), BsonExpression.Path(BsonExpression.Root(), "age"));
//var expr = Between(Path(Root(), "_id"), MakeArray(new BsonExpression[] { Constant(2), Constant(20) }));
var expr2 = And(Path(Root(), "_id"), Parameter("qrcode"));


var result = expr.Execute(doc, parameters);


Console.WriteLine(result.ToString());
Console.WriteLine(result.Type);



Console.WriteLine("\n\nEnd");
