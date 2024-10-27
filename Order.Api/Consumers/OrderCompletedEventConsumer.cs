using MassTransit;
using Order.Api.Context;
using Order.Api.Enums;
using Shared.OrderEvents;

namespace Order.Api.Consumers;

public class OrderCompletedEventConsumer(OrderDbContext dbContext) : IConsumer<OrderCompletedEvent>
{
    public async Task Consume(ConsumeContext<OrderCompletedEvent> context)
    {
        var order = await dbContext.Orders.FindAsync(context.Message.OrderId);

        if (order == null)
        {
            return;
        }

        order.OrderStatus = OrderStatus.Completed;

        await dbContext.SaveChangesAsync();
    }
}