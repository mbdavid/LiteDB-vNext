global using static LiteDB.Constants;
global using LiteDB;
global using LiteDB.Engine;


var source = "{_id:10, true , -9}";

var t = new Tokenizer(source);
var t2 = new Tokenizer2(source);

var token = t.ReadToken(); // {
var next = t.LookAhead(); // _id

var token2 = t.ReadToken();






Console.WriteLine("End");