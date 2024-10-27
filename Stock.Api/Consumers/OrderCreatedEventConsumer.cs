using MassTransit;
using MongoDB.Bson.Serialization.IdGenerators;
using Shared.OrderEvents;
using Stock.Api.Services;
using MongoDB.Driver;
using Shared.Settings;
using Shared.StockEvents;

namespace Stock.Api.Consumers;

public class OrderCreatedEventConsumer(MongoDbService mongoDbService, ISendEndpointProvider sendEndpointProvider)
    : IConsumer<OrderCreatedEvent>
{
    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        List<bool> stockResults = new();

        var stockCollection = mongoDbService.GetCollection<Models.Stock>();

        foreach (var orderItem in context.Message.OrderItems)
        {
            var stocksInWarehouse = await stockCollection.FindAsync(c =>
                c.ProductId == orderItem.ProductId && c.Count >= orderItem.Count);

            stockResults.Add(await stocksInWarehouse.AnyAsync());
        }

        var sendEndpoint =
            await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{RabbitMQSettings.StateMachineQueue}"));

        if (stockResults.TrueForAll(c => c.Equals(true)))
        {
            foreach (var orderItem in context.Message.OrderItems)
            {
                var stocksInWarehouse = await stockCollection.FindAsync(c =>
                    c.ProductId == orderItem.ProductId && c.Count >= orderItem.Count);

                var stockInWarehouse = await stocksInWarehouse.FirstOrDefaultAsync();

                stockInWarehouse.Count -= orderItem.Count;

                await stockCollection.FindOneAndReplaceAsync(c => c.ProductId == stockInWarehouse.ProductId,
                    stockInWarehouse);
            }

            StockReservedEvent stockReservedEvent = new(context.Message.CorrelationId)
            {
                OrderItems = context.Message.OrderItems
            };

            await sendEndpoint.Send(stockReservedEvent);
        }
        else
        {
            StockNotReservedEvent stockNotReservedEvent = new(context.Message.CorrelationId)
            {
                Message = "Stok yetersiz."
            };

            await sendEndpoint.Send(stockNotReservedEvent);
        }
    }
}