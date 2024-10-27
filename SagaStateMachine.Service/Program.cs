using MassTransit;
using Microsoft.EntityFrameworkCore;
using SagaStateMachine.Service.StateDbContexts;
using SagaStateMachine.Service.StateInstances;
using SagaStateMachine.Service.StateMachines;
using Shared.Settings;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMassTransit(configurator =>
{
    configurator.AddSagaStateMachine<OrderStateMachine, OrderStateInstance>()
        .EntityFrameworkRepository(options =>
        {
            options.AddDbContext<DbContext, OrderStateDbContext>((provider, optionsBuilder) =>
            {
                optionsBuilder.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
            });
        });

    configurator.UsingRabbitMq((context, factoryConfigurator) =>
    {
        factoryConfigurator.Host(builder.Configuration["RabbitMQ"]);

        factoryConfigurator.ReceiveEndpoint(RabbitMQSettings.StateMachineQueue,
            builder => builder.ConfigureSaga<OrderStateInstance>(context));
    });
});

var host = builder.Build();

host.Run();