using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SagaStateMachine.Service.StateInstances;

namespace SagaStateMachine.Service.StateMaps;

public class OrderStateMap : SagaClassMap<OrderStateInstance>
{
  protected override void Configure(EntityTypeBuilder<OrderStateInstance> entity, ModelBuilder model)
  {
      entity.Property(c => c.BuyerId).IsRequired();
      entity.Property(c => c.OrderId).IsRequired();
      entity.Property(c => c.TotalPrice).HasDefaultValue(0);
  }
}