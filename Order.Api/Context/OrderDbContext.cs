using Microsoft.EntityFrameworkCore;
using Order.Api.Models;

namespace Order.Api.Context;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Models.Order> Orders { get; set; }
    public DbSet<Models.OrderItem> OrderItems { get; set; }
}