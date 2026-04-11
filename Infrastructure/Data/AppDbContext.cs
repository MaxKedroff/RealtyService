
using Core.Models;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Building> Buildings { get; set; }
        public DbSet<Flat> Flats { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Metro> Metros { get; set; }
        public DbSet<Favourite> Favourites { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Building>()
                .HasKey(b => b.BuildingId);

            modelBuilder.Entity<Building>()
                .HasMany(b => b.Flats)
                .WithOne(f => f.Building)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Flat>()
                .HasKey(f => f.FlatId);

            modelBuilder.Entity<Flat>()
                .HasOne(f => f.Building)
                .WithMany(b => b.Flats)
                .HasForeignKey(f => f.BuildingId);

            modelBuilder.Entity<Flat>()
                .HasOne(f => f.City)
                .WithMany(c => c.Flats)
                .HasForeignKey(f => f.CityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Flat>()
                .HasIndex(f => f.BuildingId);

            modelBuilder.Entity<Flat>()
                .HasIndex(f => f.CityId);

            modelBuilder.Entity<Flat>()
                .HasIndex(f => new { f.CityId, f.IsActive });
        }
    }
}
