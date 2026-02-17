using AgroSolutions.Shared.Messages;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace AgroSolutions.AlertService;

// -------------------------------------------------------
// DBCONTEXT DO ALERT SERVICE
// -------------------------------------------------------
public class AlertDbContext : DbContext
{
    public AlertDbContext(DbContextOptions<AlertDbContext> options) : base(options) { }

    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SensorReading>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SoilMoisture).HasPrecision(5, 2);
            e.Property(x => x.Temperature).HasPrecision(5, 2);
            e.Property(x => x.Precipitation).HasPrecision(5, 2);
            e.HasIndex(x => new { x.PlotId, x.Timestamp });
        });

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

// -------------------------------------------------------
// DBCONTEXT DO PROPERTY SERVICE (para gravar leituras e alertas lá também)
// -------------------------------------------------------
public class PropertyDbContext : DbContext
{
    public PropertyDbContext(DbContextOptions<PropertyDbContext> options) : base(options) { }

    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aponta para as mesmas tabelas do PropertyService
        modelBuilder.Entity<SensorReading>().ToTable("SensorReadings").HasKey(x => x.Id);
        modelBuilder.Entity<Alert>().ToTable("Alerts").HasKey(x => x.Id);
    }
}

// -------------------------------------------------------
// MODELOS
// -------------------------------------------------------
public class SensorReading
{
    public Guid Id { get; set; }
    public Guid PlotId { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal SoilMoisture { get; set; }
    public decimal Temperature { get; set; }
    public decimal Precipitation { get; set; }
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
}

// -------------------------------------------------------
// CONSUMER
// -------------------------------------------------------
public class SensorDataConsumer : IConsumer<SensorDataReceived>
{
    private readonly AlertDbContext _alertContext;
    private readonly PropertyDbContext _propertyContext;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<SensorDataConsumer> _logger;

    public SensorDataConsumer(
        AlertDbContext alertContext,
        PropertyDbContext propertyContext,
        IPublishEndpoint publishEndpoint,
        ILogger<SensorDataConsumer> logger)
    {
        _alertContext = alertContext;
        _propertyContext = propertyContext;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SensorDataReceived> context)
    {
        var data = context.Message;

        _logger.LogInformation(
            "Processing sensor data - PlotId: {PlotId} | Moisture: {Moisture}% | Temp: {Temp}°C | Precip: {Precip}mm",
            data.PlotId, data.SoilMoisture, data.Temperature, data.Precipitation);

        // Salva leitura no AlertDb (para histórico de regras)
        var alertReading = new SensorReading
        {
            Id = data.Id,
            PlotId = data.PlotId,
            Timestamp = data.Timestamp,
            SoilMoisture = data.SoilMoisture,
            Temperature = data.Temperature,
            Precipitation = data.Precipitation
        };
        _alertContext.SensorReadings.Add(alertReading);
        await _alertContext.SaveChangesAsync();

        // Salva leitura no PropertyDb (para dashboard/visualização)
        try
        {
            var propertyReading = new SensorReading
            {
                Id = Guid.NewGuid(), // novo Id para o PropertyDb
                PlotId = data.PlotId,
                Timestamp = data.Timestamp,
                SoilMoisture = data.SoilMoisture,
                Temperature = data.Temperature,
                Precipitation = data.Precipitation
            };
            _propertyContext.SensorReadings.Add(propertyReading);
            await _propertyContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log mas não falha o processamento - o plotId pode não existir ainda
            _logger.LogWarning(ex, "Could not save reading to PropertyDb for PlotId {PlotId}. Plot may not exist.", data.PlotId);
        }

        // Processar regras de alerta
        await CheckDroughtAlertAsync(data);
        await CheckHeatAlertAsync(data);
        await CheckPestAlertAsync(data);
    }

    // -----------------------------------------------------------
    // REGRA 1: Alerta de Seca
    // Umidade < 30% por mais de 24 horas
    // -----------------------------------------------------------
    private async Task CheckDroughtAlertAsync(SensorDataReceived data)
    {
        const decimal droughtThreshold = 30m;
        const int durationHours = 24;

        if (data.SoilMoisture < droughtThreshold)
        {
            var windowStart = DateTime.UtcNow.AddHours(-durationHours);

            var totalReadings = await _alertContext.SensorReadings
                .CountAsync(r => r.PlotId == data.PlotId && r.Timestamp >= windowStart);

            var lowMoistureReadings = await _alertContext.SensorReadings
                .CountAsync(r => r.PlotId == data.PlotId
                    && r.Timestamp >= windowStart
                    && r.SoilMoisture < droughtThreshold);

            _logger.LogDebug("Drought check: {Low}/{Total} low readings in last {Hours}h for plot {PlotId}",
                lowMoistureReadings, totalReadings, durationHours, data.PlotId);

            // Dispara se >= 80% das leituras das últimas 24h estão abaixo do threshold
            // OU se for a única leitura e umidade está crítica (< 15%)
            var shouldAlert = (totalReadings >= 10 && lowMoistureReadings >= totalReadings * 0.8)
                           || (data.SoilMoisture < 15); // emergência imediata

            if (shouldAlert)
            {
                var existingAlert = await _alertContext.Alerts
                    .AnyAsync(a => a.PlotId == data.PlotId
                        && a.AlertType == "Seca"
                        && !a.IsResolved);

                if (!existingAlert)
                {
                    await CreateAlertAsync(
                        data.PlotId,
                        alertType: "Seca",
                        message: $"Alerta de Seca: Umidade do solo abaixo de {droughtThreshold}% " +
                                 $"nas últimas {durationHours} horas. Valor atual: {data.SoilMoisture:F1}%",
                        severity: "Critical"
                    );
                }
            }
        }
        else if (data.SoilMoisture >= 35)
        {
            // Resolve alertas de seca
            await ResolveAlertsAsync(data.PlotId, "Seca");
        }
    }

    // -----------------------------------------------------------
    // REGRA 2: Alerta de Calor Excessivo
    // Temperatura > 35°C por mais de 12 horas
    // -----------------------------------------------------------
    private async Task CheckHeatAlertAsync(SensorDataReceived data)
    {
        const decimal heatThreshold = 35m;
        const int durationHours = 12;

        if (data.Temperature > heatThreshold)
        {
            var windowStart = DateTime.UtcNow.AddHours(-durationHours);

            var totalReadings = await _alertContext.SensorReadings
                .CountAsync(r => r.PlotId == data.PlotId && r.Timestamp >= windowStart);

            var highTempReadings = await _alertContext.SensorReadings
                .CountAsync(r => r.PlotId == data.PlotId
                    && r.Timestamp >= windowStart
                    && r.Temperature > heatThreshold);

            var shouldAlert = (totalReadings >= 5 && highTempReadings >= totalReadings * 0.8)
                           || data.Temperature > 42; // emergência imediata

            if (shouldAlert)
            {
                var existingAlert = await _alertContext.Alerts
                    .AnyAsync(a => a.PlotId == data.PlotId
                        && a.AlertType == "Calor Excessivo"
                        && !a.IsResolved);

                if (!existingAlert)
                {
                    await CreateAlertAsync(
                        data.PlotId,
                        alertType: "Calor Excessivo",
                        message: $"Alerta de Calor: Temperatura acima de {heatThreshold}°C " +
                                 $"por mais de {durationHours} horas. Valor atual: {data.Temperature:F1}°C",
                        severity: "Warning"
                    );
                }
            }
        }
        else if (data.Temperature <= 32)
        {
            await ResolveAlertsAsync(data.PlotId, "Calor Excessivo");
        }
    }

    // -----------------------------------------------------------
    // REGRA 3: Risco de Praga
    // Umidade > 80% E temperatura entre 20°C e 30°C
    // -----------------------------------------------------------
    private async Task CheckPestAlertAsync(SensorDataReceived data)
    {
        if (data.SoilMoisture > 80 && data.Temperature is >= 20 and <= 30)
        {
            // Evita criar alerta duplicado nas últimas 48h
            var recentAlert = await _alertContext.Alerts
                .AnyAsync(a => a.PlotId == data.PlotId
                    && a.AlertType == "Risco de Praga"
                    && !a.IsResolved
                    && a.CreatedAt >= DateTime.UtcNow.AddHours(-48));

            if (!recentAlert)
            {
                await CreateAlertAsync(
                    data.PlotId,
                    alertType: "Risco de Praga",
                    message: $"Condições favoráveis a pragas: " +
                             $"Umidade {data.SoilMoisture:F1}% e Temperatura {data.Temperature:F1}°C",
                    severity: "Warning"
                );
            }
        }
    }

    // -----------------------------------------------------------
    // HELPERS
    // -----------------------------------------------------------
    private async Task CreateAlertAsync(Guid plotId, string alertType, string message, string severity)
    {
        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            PlotId = plotId,
            AlertType = alertType,
            Message = message,
            Severity = severity,
            CreatedAt = DateTime.UtcNow,
            IsResolved = false
        };

        _alertContext.Alerts.Add(alert);
        await _alertContext.SaveChangesAsync();

        // Grava também no PropertyDb para visualização no dashboard
        try
        {
            var propertyAlert = new Alert
            {
                Id = Guid.NewGuid(),
                PlotId = plotId,
                AlertType = alertType,
                Message = message,
                Severity = severity,
                CreatedAt = DateTime.UtcNow,
                IsResolved = false
            };
            _propertyContext.Alerts.Add(propertyAlert);
            await _propertyContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not save alert to PropertyDb for PlotId {PlotId}", plotId);
        }

        // Publica evento
        await _publishEndpoint.Publish(new AlertCreated
        {
            Id = alert.Id,
            PlotId = plotId,
            AlertType = alertType,
            Message = message,
            CreatedAt = alert.CreatedAt,
            Severity = severity
        });

        _logger.LogWarning("ALERT [{Severity}] {AlertType} created for PlotId: {PlotId} | {Message}",
            severity, alertType, plotId, message);
    }

    private async Task ResolveAlertsAsync(Guid plotId, string alertType)
    {
        var alerts = await _alertContext.Alerts
            .Where(a => a.PlotId == plotId && a.AlertType == alertType && !a.IsResolved)
            .ToListAsync();

        if (alerts.Any())
        {
            foreach (var alert in alerts)
                alert.IsResolved = true;

            await _alertContext.SaveChangesAsync();

            // Resolve no PropertyDb também
            try
            {
                var propertyAlerts = await _propertyContext.Alerts
                    .Where(a => a.PlotId == plotId && a.AlertType == alertType && !a.IsResolved)
                    .ToListAsync();

                foreach (var a in propertyAlerts)
                    a.IsResolved = true;

                await _propertyContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not resolve alerts in PropertyDb");
            }

            _logger.LogInformation("Alert [{AlertType}] resolved for PlotId: {PlotId}", alertType, plotId);
        }
    }
}