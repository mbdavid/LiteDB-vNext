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
    },
    ["primary"] = 51,
    ["phones"] = new BsonArray
    {
        new BsonDocument { ["ddd"] = 51, ["number"] = "123" },
        new BsonDocument { ["ddd"] = 11, ["number"] = "456" },
        new BsonDocument { ["ddd"] = 21, ["number"] = "789" },
    }
};

var parameters = new BsonDocument
{
    ["qrcode"] = "237467846234263",
    ["usuario"] = new BsonDocument
    {
        ["_id"] = 15,
        ["nome"] = "jose",
    }
};


//BsonExpression expr = "$"

// _id, age


var exprA = Between(Path(Root(), "age"), MakeArray(new BsonExpression[] { Constant(30), Constant(50) }));
var exprB = Create("age between 30 and 50");

Console.WriteLine(exprA == exprB);

Console.WriteLine("Expr A: " + exprA.ToString());
Console.WriteLine("Expr B: " + exprB.ToString());


var result = exprA.Execute(doc, parameters);

//Console.WriteLine(exprB.ToString());
//Console.WriteLine("-----------------------------");
Console.WriteLine("Result: " + result.ToString());
//Console.WriteLine(result.Type);
//


Console.WriteLine("\n\nEnd");
