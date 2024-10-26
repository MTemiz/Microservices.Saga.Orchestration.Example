using MongoDB.Driver;

namespace Stock.Api.Services;

public class MongoDbService
{
    private readonly IMongoDatabase _database;

    public MongoDbService(IConfiguration configuration)
    {
        MongoClient client = new MongoClient(configuration.GetConnectionString("StockDb"));
    }

    public IMongoCollection<T> GetCollection<T>()
    {
        return _database.GetCollection<T>(typeof(T).Name.ToLowerInvariant());
    }
}