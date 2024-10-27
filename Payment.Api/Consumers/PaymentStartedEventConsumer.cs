using MassTransit;
using Shared.PaymentEvents;
using Shared.Settings;

namespace Payment.Api.Consumers;

public class PaymentStartedEventConsumer(ISendEndpointProvider sendEndpointProvider) : IConsumer<PaymentStartedEvent>
{
    public async Task Consume(ConsumeContext<PaymentStartedEvent> context)
    {
        bool paymentResult = true;

        var sendEndpoint =
            await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{RabbitMQSettings.StateMachineQueue}"));

        if (paymentResult)
        {
            PaymentCompletedEvent paymentCompleted = new(context.Message.CorrelationId);

            await sendEndpoint.Send(paymentCompleted);
        }
        else
        {
            PaymentFailedEvent paymentFailedEvent = new(context.Message.CorrelationId)
            {
                OrderItems = context.Message.OrderItems,
                Message = "Bakiye yetersiz."
            };
            
            await sendEndpoint.Send(paymentFailedEvent);
        }
    }
}