using Microsoft.EntityFrameworkCore;
using PizzaStore.Models;

namespace PizzaStore.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Pizza> Pizzas { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Pizza>().HasData(
                new Pizza { Id = 1, Name = "Pepperoni", Description = "Classic Pepperoni Pizza" });
        }
    }
}
