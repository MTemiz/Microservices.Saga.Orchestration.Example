using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Stock.Api.Models;

public class Stock
{
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonId]
    [BsonElement(Order = 0)]
    public ObjectId Id { get; set; }

    [BsonRepresentation(BsonType.Int64)]
    [BsonElement(Order = 1)]
    public int ProductId { get; set; }

    [BsonRepresentation(BsonType.Int64)]
    [BsonElement(Order = 2)]
    public int Count { get; set; }
}