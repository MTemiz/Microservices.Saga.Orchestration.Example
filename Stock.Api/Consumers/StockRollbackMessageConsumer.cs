using MassTransit;
using Shared.Messages;
using Stock.Api.Services;
using MongoDB.Driver;

namespace Stock.Api.Consumers;

public class StockRollbackMessageConsumer(MongoDbService mongoDbService) : IConsumer<StockRollbackMessage>
{
    public async Task Consume(ConsumeContext<StockRollbackMessage> context)
    {
        var stockCollection = mongoDbService.GetCollection<Models.Stock>();

        foreach (var orderItem in context.Message.OrderItems)
        {
            var stocksInWareHouse = await stockCollection.FindAsync(c => c.ProductId == orderItem.ProductId);

            var stockInWarehouse = await stocksInWareHouse.FirstOrDefaultAsync();

            stockInWarehouse.Count += orderItem.Count;

            await stockCollection.FindOneAndReplaceAsync(c => c.ProductId == orderItem.ProductId, stockInWarehouse);
        }
    }
}