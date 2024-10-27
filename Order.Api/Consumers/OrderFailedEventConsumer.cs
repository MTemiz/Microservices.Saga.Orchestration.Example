using MassTransit;
using Order.Api.Context;
using Order.Api.Enums;
using Shared.OrderEvents;

namespace Order.Api.Consumers;

public class OrderFailedEventConsumer(OrderDbContext dbContext) : IConsumer<OrderFailedEvent>
{
    public async Task Consume(ConsumeContext<OrderFailedEvent> context)
    {
        var order = await dbContext.Orders.FindAsync(context.Message.OrderId);

        if (order == null)
        {
            return;
        }

        order.OrderStatus = OrderStatus.Fail;

        await dbContext.SaveChangesAsync();
    }
}