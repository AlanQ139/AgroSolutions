using Microsoft.EntityFrameworkCore;

namespace AgroSolutions.PropertyService.Data;

// -------------------------------------------------------
// ENTIDADES
// -------------------------------------------------------

public class Property
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal TotalArea { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public List<Plot> Plots { get; set; } = new();
}

public class Plot
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CropType { get; set; } = string.Empty;
    public decimal Area { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Property Property { get; set; } = null!;
    public List<SensorReading> SensorReadings { get; set; } = new();
}

public class SensorReading
{
    public Guid Id { get; set; }
    public Guid PlotId { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal SoilMoisture { get; set; }
    public decimal Temperature { get; set; }
    public decimal Precipitation { get; set; }

    // Navigation
    public Plot Plot { get; set; } = null!;
}

public class Alert
{
    public Guid Id { get; set; }
    public Guid PlotId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Warning";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsResolved { get; set; } = false;

    // Navigation
    public Plot Plot { get; set; } = null!;
}

// -------------------------------------------------------
// DBCONTEXT
// -------------------------------------------------------

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Plot> Plots => Set<Plot>();
    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ---- Property ----
        modelBuilder.Entity<Property>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Location).HasMaxLength(500);
            e.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            e.Property(x => x.TotalArea).HasPrecision(10, 2);

            e.HasMany(x => x.Plots)
             .WithOne(x => x.Property)
             .HasForeignKey(x => x.PropertyId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ---- Plot ----
        modelBuilder.Entity<Plot>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.CropType).IsRequired().HasMaxLength(100);
            e.Property(x => x.Area).HasPrecision(10, 2);

            e.HasMany(x => x.SensorReadings)
             .WithOne(x => x.Plot)
             .HasForeignKey(x => x.PlotId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany<Alert>()
             .WithOne(x => x.Plot)
             .HasForeignKey(x => x.PlotId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ---- SensorReading ----
        modelBuilder.Entity<SensorReading>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SoilMoisture).HasPrecision(5, 2);
            e.Property(x => x.Temperature).HasPrecision(5, 2);
            e.Property(x => x.Precipitation).HasPrecision(5, 2);
            e.HasIndex(x => new { x.PlotId, x.Timestamp });
        });

        // ---- Alert ----
        modelBuilder.Entity<Alert>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.AlertType).IsRequired().HasMaxLength(100);
            e.Property(x => x.Message).IsRequired().HasMaxLength(1000);
            e.Property(x => x.Severity).HasMaxLength(50);
            e.HasIndex(x => new { x.PlotId, x.CreatedAt });
        });
    }
}