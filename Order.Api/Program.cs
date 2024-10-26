using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.Api.Context;
using Order.Api.Enums;
using Order.Api.Models;
using Order.Api.ViewModels;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<OrderDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
});

builder.Services.AddMassTransit(configurator =>
{
    configurator.UsingRabbitMq((context, factoryConfigurator) =>
    {
        factoryConfigurator.Host(builder.Configuration["RabbitMQ"]);
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/create-order", async (OrderDbContext dbContext, CreateOrderVM model) =>
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
});
app.Run();