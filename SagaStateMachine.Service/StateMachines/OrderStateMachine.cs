using MassTransit;
using SagaStateMachine.Service.StateInstances;
using Shared.Messages;
using Shared.OrderEvents;
using Shared.PaymentEvents;
using Shared.Settings;
using Shared.StockEvents;

namespace SagaStateMachine.Service.StateMachines;

public class OrderStateMachine : MassTransitStateMachine<OrderStateInstance>
{
    public Event<OrderStartedEvent> OrderStartedEvent { get; set; }
    public Event<StockReservedEvent> StockReservedEvent { get; set; }
    public Event<StockNotReservedEvent> StockNotReservedEvent { get; set; }
    public Event<PaymentCompletedEvent> PaymentCompletedEvent { get; set; }
    public Event<PaymentFailedEvent> PaymentFailedEvent { get; set; }

    public Event<OrderCancelledEvent> OrderCancelledEvent { get; set; }

    public State OrderCreated { get; set; }
    public State StockReserved { get; set; }
    public State StockNotReserved { get; set; }
    public State PaymentCompleted { get; set; }
    public State PaymentFailed { get; set; }

    public State OrderCancelled { get; set; }

    public OrderStateMachine()
    {
        InstanceState(instance => instance.CurrentState);

        Event(() => OrderStartedEvent,
            orderStateInstance =>
                orderStateInstance.CorrelateBy<int>(database => database.OrderId, @event => @event.Message.OrderId)
                    .SelectId(e => Guid.NewGuid()));

        Event(() => OrderCancelledEvent,
            orderStateInstance => orderStateInstance
                .CorrelateBy<int>(database => database.OrderId, @event => @event.Message.OrderId));

        Event(() => StockReservedEvent,
            orderStateInstance => orderStateInstance.CorrelateById(@event => @event.Message.CorrelationId));

        Event(() => StockNotReservedEvent,
            orderStateInstance => orderStateInstance.CorrelateById(@event => @event.Message.CorrelationId));

        Event(() => PaymentCompletedEvent,
            orderStateInstance => orderStateInstance.CorrelateById(@event => @event.Message.CorrelationId));

        Event(() => PaymentFailedEvent,
            orderStateInstance => orderStateInstance.CorrelateById(@event => @event.Message.CorrelationId));

        Initially(
            When(OrderStartedEvent)
                .Then(context =>
                {
                    context.Instance.OrderId = context.Data.OrderId;
                    context.Instance.BuyerId = context.Data.BuyerId;
                    context.Instance.CreatedDate = DateTime.Now;
                    context.Instance.TotalPrice = context.Data.TotalPrice;
                })
                .Then(context => Console.WriteLine("İŞLEM 1"))
                .TransitionTo(OrderCreated)
                .Send(new Uri($"queue:{RabbitMQSettings.Stock_OrderCreatedEventQueue}"),
                    context => new OrderCreatedEvent(context.Instance.CorrelationId)
                    {
                        OrderItems = context.Data.OrderItems,
                    }
                ),
            When(OrderCancelledEvent)
                .TransitionTo(OrderCancelled)
                .Send(new Uri($"queue:{RabbitMQSettings.Order_OrderFailedEventQueue}"), context =>
                    new OrderFailedEvent()
                    {
                        OrderId = context.Data.OrderId,
                    })
        );

        During(OrderCreated,
            When(StockReservedEvent)
                .TransitionTo(StockReserved)
                .Send(new Uri($"queue:{RabbitMQSettings.Payment_StartedEventQueue}"),
                    context => new PaymentStartedEvent(context.Instance.CorrelationId)
                    {
                        OrderItems = context.Data.OrderItems,
                        TotalPrice = context.Instance.TotalPrice
                    }),
            When(StockNotReservedEvent)
                .TransitionTo(StockNotReserved)
                .Send(new Uri($"queue:{RabbitMQSettings.Order_OrderFailedEventQueue}"),
                    context => new OrderFailedEvent
                    {
                        OrderId = context.Instance.OrderId,
                        Message = context.Data.Message
                    }
                ),
            When(OrderCancelledEvent)
                .TransitionTo(OrderCancelled)
                .Send(new Uri($"queue:{RabbitMQSettings.Order_OrderFailedEventQueue}"), context =>
                    new OrderFailedEvent()
                    {
                        OrderId = context.Data.OrderId,
                    })
        );

        During(StockReserved,
            When(PaymentCompletedEvent)
                .TransitionTo(PaymentCompleted)
                .Send(new Uri($"queue:{RabbitMQSettings.Order_OrderCompletedEventQueue}"),
                    context => new OrderCompletedEvent()
                    {
                        OrderId = context.Instance.OrderId,
                    })
                .Finalize(),
            When(PaymentFailedEvent)
                .TransitionTo(PaymentFailed)
                .Send(new Uri($"queue:{RabbitMQSettings.Order_OrderFailedEventQueue}"), context =>
                    new OrderFailedEvent()
                    {
                        OrderId = context.Instance.OrderId,
                        Message = context.Data.Message
                    })
                .Send(new Uri($"queue:{RabbitMQSettings.Stock_RollbackMessageQueue}"), context =>
                    new StockRollbackMessage()
                    {
                        OrderItems = context.Data.OrderItems
                    })
        );

        SetCompletedWhenFinalized();
    }
}