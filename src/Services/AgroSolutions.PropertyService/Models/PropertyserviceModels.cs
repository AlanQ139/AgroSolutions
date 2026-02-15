using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace AgroSolutions.PropertyService.Data;

public class Property
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal TotalArea { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Plot> Plots { get; set; } = new();
}

public class Plot
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CropType { get; set; } = string.Empty; // Cultura plantada
    public decimal Area { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Property Property { get; set; } = null!;
    public List<SensorData> SensorData { get; set; } = new();
}

public class SensorData
{
    public Guid Id { get; set; }
    public Guid PlotId { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal SoilMoisture { get; set; }
    public decimal Temperature { get; set; }
    public decimal Precipitation { get; set; }

    public Plot Plot { get; set; } = null!;
}

public class Alert
{
    public Guid Id { get; set; }
    public Guid PlotId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Warning";
    public DateTime CreatedAt { get; set; }
    public bool IsResolved { get; set; }

    public Plot Plot { get; set; } = null!;
}

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Plot> Plots => Set<Plot>();
    public DbSet<SensorData> SensorData => Set<SensorData>();
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Property>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Location).HasMaxLength(500);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.HasMany(e => e.Plots).WithOne(e => e.Property).HasForeignKey(e => e.PropertyId);
        });

        modelBuilder.Entity<Plot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CropType).IsRequired().HasMaxLength(100);
            entity.HasMany(e => e.SensorData).WithOne(e => e.Plot).HasForeignKey(e => e.PlotId);
        });

        modelBuilder.Entity<SensorData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SoilMoisture).HasPrecision(5, 2);
            entity.Property(e => e.Temperature).HasPrecision(5, 2);
            entity.Property(e => e.Precipitation).HasPrecision(5, 2);
            entity.HasIndex(e => new { e.PlotId, e.Timestamp });
        });

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AlertType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Severity).HasMaxLength(50);
            entity.HasIndex(e => new { e.PlotId, e.CreatedAt });
        });
    }
}