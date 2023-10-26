namespace LiteDB.Tests.Document;

public class Document_InvalidOperations_Tests
{
    [Fact]
    public void BsonDocument_WritingToReadOnly_ShouldThrowException()
    {

        //Arrange
        var d = BsonDocument.Empty;

        //Act + Assert
        Assert.Throws<NotSupportedException>(() => d["Name"] = "Rodolfo");                      //Try changing existing data
        Assert.Throws<NotSupportedException>(() => d["Age"] = 26);                              //Try creating data
        Assert.Throws<NotSupportedException>( () => d.Add("key", new BsonString("value")) );    //Try creating data

    }



}
