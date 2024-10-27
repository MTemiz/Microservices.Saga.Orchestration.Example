using MassTransit;
using Payment.Api.Consumers;
using Shared.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(configurator =>
{
    configurator.AddConsumer<PaymentStartedEventConsumer>();

    configurator.UsingRabbitMq((context, factoryConfigurator) =>
    {
        factoryConfigurator.Host(builder.Configuration["RabbitMQ"]);

        factoryConfigurator.ReceiveEndpoint(RabbitMQSettings.Payment_StartedEventQueue,
            e => e.ConfigureConsumer<PaymentStartedEventConsumer>(context));
    });
});

var app = builder.Build();

app.Run();