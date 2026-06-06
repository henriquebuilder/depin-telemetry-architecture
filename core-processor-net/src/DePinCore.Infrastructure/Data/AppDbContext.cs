using DePinCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DePinCore.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Node> Nodes { get; set; }
    public DbSet<NodeTelemetry> NodeTelemetries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Node>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DeviceId).IsUnique();
            entity.Property(e => e.DeviceId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.DeviceType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Location).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CurrentHealthStatus).HasConversion<string>();
            entity.HasMany(e => e.TelemetryHistory)
                  .WithOne()
                  .HasForeignKey(t => t.DeviceId)
                  .HasPrincipalKey(n => n.DeviceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NodeTelemetry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.Timestamp);
            entity.Property(e => e.DeviceId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.DeviceType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Location).IsRequired().HasMaxLength(255);
            entity.Property(e => e.HealthStatus).HasConversion<string>();
            entity.Property(e => e.Metrics).HasColumnType("jsonb");
        });
    }
}
