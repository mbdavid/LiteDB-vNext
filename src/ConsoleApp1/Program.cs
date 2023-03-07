using LiteDB;
using LiteDB.Engine;

var filename = @"C:\LiteDB\temp\db1.db";

File.Delete(filename);

using var db = new LiteEngine(filename);

db.OpenAsync();

Console.WriteLine("Database open");

Console.ReadKey();