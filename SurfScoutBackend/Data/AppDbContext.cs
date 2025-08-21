using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using SurfScoutBackend.Models;
using SurfScoutBackend.Models.WindFieldModel;

namespace SurfScoutBackend.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> users { get; set; }
        public DbSet<UserConnection> userconnections { get; set; }
        public DbSet<Spot> spots { get; set; }
        public DbSet<Session> sessions { get; set; }
        public DbSet<WindField> windfields { get; set; }
        public DbSet<WindFieldPoint> windfieldpoints { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Session>().ToTable("sessions");
            modelBuilder.Entity<Spot>().ToTable("spots");
            modelBuilder.Entity<WindField>().ToTable("windfields");
            modelBuilder.Entity<WindFieldPoint>().ToTable("windfieldpoints");

            modelBuilder.Entity<UserConnection>().ToTable("userconnections");

            modelBuilder.Entity<UserConnection>()
                .HasKey(uc => new { uc.RequesterId, uc.AddresseeId });

            modelBuilder.Entity<UserConnection>()
                .HasOne(uc => uc.Requester)
                .WithMany()
                .HasForeignKey(uc => uc.RequesterId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserConnection>()
                .HasOne(uc => uc.Addressee)
                .WithMany()
                .HasForeignKey(uc => uc.AddresseeId)
                .OnDelete(DeleteBehavior.Cascade);

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

            modelBuilder.Entity<WindField>()
                .HasOne(w => w.Session)
                .WithMany(s => s.WindFields)
                .HasForeignKey(w => w.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WindFieldPoint>()
                .HasOne(p => p.WindField)
                .WithMany(w => w.Points)
                .HasForeignKey(p => p.WindFieldId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WindFieldPoint>()
                .Property(p => p.Location)
                .HasColumnType("geometry(Point,4326)");
        }
    }
}
