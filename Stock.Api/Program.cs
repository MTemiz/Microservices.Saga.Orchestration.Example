using MassTransit;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Stock.Api.Services;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<MongoDbService>();

builder.Services.AddMassTransit(configurator =>
{
    configurator.UsingRabbitMq((context, factoryConfigurator) =>
    {
        factoryConfigurator.Host(builder.Configuration["RabbitMQ"]);
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