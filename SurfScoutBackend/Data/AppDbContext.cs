using Microsoft.EntityFrameworkCore;
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

            modelBuilder.Entity<Session>()
                .HasOne(s => s.user)
                .WithMany(u => u.sessions)
                .HasForeignKey(s => s.userId);
        }
    }
}
