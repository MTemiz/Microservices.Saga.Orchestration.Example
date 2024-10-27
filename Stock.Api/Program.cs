using MassTransit;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Stock.Api.Services;
using MongoDB.Driver;
using Shared.Settings;
using Stock.Api.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<MongoDbService>();

builder.Services.AddMassTransit(configurator =>
{
    configurator.AddConsumer<OrderCreatedEventConsumer>();
    configurator.AddConsumer<StockRollbackMessageConsumer>();
    
    configurator.UsingRabbitMq((context, factoryConfigurator) =>
    {
        factoryConfigurator.Host(builder.Configuration["RabbitMQ"]);
        
        factoryConfigurator.ReceiveEndpoint(RabbitMQSettings.Stock_OrderCreatedEventQueue,
            e => e.ConfigureConsumer<OrderCreatedEventConsumer>(context));
        
        factoryConfigurator.ReceiveEndpoint(RabbitMQSettings.Stock_RollbackMessageQueue,
            e => e.ConfigureConsumer<StockRollbackMessageConsumer>(context));
    });
});

var app = builder.Build();

using IServiceScope serviceScope = app.Services.CreateScope();

var service = serviceScope.ServiceProvider.GetRequiredService<MongoDbService>();

var stockCollection = service.GetCollection<Stock.Api.Models.Stock>();

var stocksInWarehouse = await stockCollection.FindAsync(c => true);

if (!stocksInWarehouse.Any())
{
    await stockCollection.InsertOneAsync(new Stock.Api.Models.Stock() { ProductId = 1, Count = 1000 });
    await stockCollection.InsertOneAsync(new Stock.Api.Models.Stock() { ProductId = 2, Count = 2000 });
    await stockCollection.InsertOneAsync(new Stock.Api.Models.Stock() { ProductId = 3, Count = 3000 });
}

app.Run();