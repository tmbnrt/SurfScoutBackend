using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using SurfScoutBackend.Models;

namespace SurfScoutBackend.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> users { get; set; }
        public DbSet<Spot> spots { get; set; }
        public DbSet<Session> sessions { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Session>().ToTable("sessions");
            modelBuilder.Entity<Spot>().ToTable("spots");

            modelBuilder.Entity<Spot>()
                .Property(s => s.Location)
                .HasColumnType("geometry(Point,4326)");

            modelBuilder.Entity<Session>()
                .HasOne(s => s.User)
                .WithMany(u => u.Sessions)
                .HasForeignKey(s => s.UserId);

            modelBuilder.Entity<Session>()
                .HasOne(s => s.Spot)                
                .WithMany(s => s.Sessions)
                .HasForeignKey(s => s.Spotid)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
