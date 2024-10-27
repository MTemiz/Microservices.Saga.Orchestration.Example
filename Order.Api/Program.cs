using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.Api.Consumers;
using Order.Api.Context;
using Order.Api.Enums;
using Order.Api.Models;
using Order.Api.ViewModels;
using Shared.Messages;
using Shared.OrderEvents;
using Shared.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<OrderDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
});

builder.Services.AddMassTransit(configurator =>
{
    configurator.AddConsumer<OrderCompletedEventConsumer>();
    configurator.AddConsumer<OrderFailedEventConsumer>();

    configurator.UsingRabbitMq((context, factoryConfigurator) =>
    {
        factoryConfigurator.Host(builder.Configuration["RabbitMQ"]);

        factoryConfigurator.ReceiveEndpoint(RabbitMQSettings.Order_OrderCompletedEventQueue,
            e => e.ConfigureConsumer<OrderCompletedEventConsumer>(context));
        
        factoryConfigurator.ReceiveEndpoint(RabbitMQSettings.Order_OrderFailedEventQueue,
            e => e.ConfigureConsumer<OrderFailedEventConsumer>(context));
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/create-order",
    async (OrderDbContext dbContext, CreateOrderVM model, ISendEndpointProvider sendEndpointProvider) =>
    {
        Order.Api.Models.Order order = new Order.Api.Models.Order()
        {
            BuyerId = model.BuyerId,
            CreatedDate = DateTime.Now,
            OrderStatus = OrderStatus.Suspend,
            TotalPrice = model.OrderItems.Sum(c => c.Price * c.Count),
            OrderItems = model.OrderItems.Select(oi =>
                new OrderItem()
                {
                    ProductId = oi.ProductId,
                    Count = oi.Count,
                    Price = oi.Price
                }).ToList()
        };

        await dbContext.Orders.AddAsync(order);
        
        await dbContext.SaveChangesAsync();

        OrderStartedEvent orderStartedEvent = new()
        {
            OrderId = order.Id,
            BuyerId = order.BuyerId,
            TotalPrice = order.TotalPrice,
            OrderItems = order.OrderItems.Select(oi => new OrderItemMessage
            {
                ProductId = oi.ProductId,
                Count = oi.Count,
                Price = oi.Price
            }).ToList()
        };

        var sendEndpoint =
            await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{RabbitMQSettings.StateMachineQueue}"));

        await sendEndpoint.Send(orderStartedEvent);
    });

app.MapPost("/fail-order/{orderId}",
    async (OrderDbContext dbContext, int orderId, ISendEndpointProvider sendEndpointProvider) =>
    {
        var order = await dbContext.Orders.FindAsync(orderId);

        OrderCancelledEvent orderCancelledEvent = new()
        {
            OrderId = order.Id,
        };

        var sendEndpoint =
            await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{RabbitMQSettings.StateMachineQueue}"));

        await sendEndpoint.Send(orderCancelledEvent);
    });

app.Run();