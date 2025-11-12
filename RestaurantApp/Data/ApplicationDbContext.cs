using Microsoft.EntityFrameworkCore;
using RestaurantApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;


namespace RestaurantApp.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }



        public DbSet<Dish> Dishes { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Dish>(entity =>
            {
                
                entity.ToTable("Dishes");
                entity.HasIndex(e => e.Name);
            });
        }
    }
    
}
