using AgroSolutions.Shared.Messages;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace AgroSolutions.AlertService;

// DbContext for AlertService
public class AlertDbContext : DbContext
{
    public AlertDbContext(DbContextOptions<AlertDbContext> options) : base(options) { }

    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SensorReading>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PlotId, e.Timestamp });
            entity.Property(e => e.SoilMoisture).HasPrecision(5, 2);
            entity.Property(e => e.Temperature).HasPrecision(5, 2);
            entity.Property(e => e.Precipitation).HasPrecision(5, 2);
        });

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AlertType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
            entity.HasIndex(e => new { e.PlotId, e.CreatedAt });
        });
    }
}

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
    public DateTime CreatedAt { get; set; }
    public bool IsResolved { get; set; }
}

// Consumer para processar eventos de sensores
public class SensorDataConsumer : IConsumer<SensorDataReceived>
{
    private readonly AlertDbContext _context;
    private readonly ILogger<SensorDataConsumer> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public SensorDataConsumer(
        AlertDbContext context,
        ILogger<SensorDataConsumer> logger,
        IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<SensorDataReceived> context)
    {
        var sensorData = context.Message;

        _logger.LogInformation(
            "Processando dados de sensor - PlotId: {PlotId}, Umidade: {Moisture}%, Temp: {Temp}°C",
            sensorData.PlotId, sensorData.SoilMoisture, sensorData.Temperature);

        // Armazena a leitura
        var reading = new SensorReading
        {
            Id = sensorData.Id,
            PlotId = sensorData.PlotId,
            Timestamp = sensorData.Timestamp,
            SoilMoisture = sensorData.SoilMoisture,
            Temperature = sensorData.Temperature,
            Precipitation = sensorData.Precipitation
        };

        _context.SensorReadings.Add(reading);
        await _context.SaveChangesAsync();

        // Processa regras de alerta
        await CheckDroughtAlert(sensorData);
        await CheckHeatAlert(sensorData);
        await CheckPestAlert(sensorData);
    }

    private async Task CheckDroughtAlert(SensorDataReceived sensorData)
    {
        // Regra: umidade < 30% por mais de 24 horas
        if (sensorData.SoilMoisture < 30)
        {
            var twentyFourHoursAgo = DateTime.UtcNow.AddHours(-24);

            var lowMoistureReadings = await _context.SensorReadings
                .Where(r => r.PlotId == sensorData.PlotId
                    && r.Timestamp >= twentyFourHoursAgo
                    && r.SoilMoisture < 30)
                .CountAsync();

            var totalReadings = await _context.SensorReadings
                .Where(r => r.PlotId == sensorData.PlotId
                    && r.Timestamp >= twentyFourHoursAgo)
                .CountAsync();

            // Se a maioria das leituras nas últimas 24h está baixa
            if (totalReadings >= 10 && lowMoistureReadings >= (totalReadings * 0.8))
            {
                // Verifica se já existe alerta ativo
                var existingAlert = await _context.Alerts
                    .FirstOrDefaultAsync(a => a.PlotId == sensorData.PlotId
                        && a.AlertType == "Seca"
                        && !a.IsResolved);

                if (existingAlert == null)
                {
                    var alert = new Alert
                    {
                        Id = Guid.NewGuid(),
                        PlotId = sensorData.PlotId,
                        AlertType = "Seca",
                        Message = $"Alerta de Seca: Umidade do solo abaixo de 30% há mais de 24 horas. Valor atual: {sensorData.SoilMoisture:F2}%",
                        Severity = "Critical",
                        CreatedAt = DateTime.UtcNow,
                        IsResolved = false
                    };

                    _context.Alerts.Add(alert);
                    await _context.SaveChangesAsync();

                    // Publica evento de alerta
                    await _publishEndpoint.Publish(new AlertCreated
                    {
                        Id = alert.Id,
                        PlotId = alert.PlotId,
                        AlertType = alert.AlertType,
                        Message = alert.Message,
                        CreatedAt = alert.CreatedAt,
                        Severity = alert.Severity
                    });

                    _logger.LogWarning("ALERTA DE SECA criado para PlotId: {PlotId}", sensorData.PlotId);
                }
            }
        }
        else if (sensorData.SoilMoisture >= 35)
        {
            // Resolve alertas de seca se umidade voltou ao normal
            var activeAlerts = await _context.Alerts
                .Where(a => a.PlotId == sensorData.PlotId
                    && a.AlertType == "Seca"
                    && !a.IsResolved)
                .ToListAsync();

            foreach (var alert in activeAlerts)
            {
                alert.IsResolved = true;
                _logger.LogInformation("Alerta de Seca resolvido para PlotId: {PlotId}", sensorData.PlotId);
            }

            await _context.SaveChangesAsync();
        }
    }

    private async Task CheckHeatAlert(SensorDataReceived sensorData)
    {
        // Regra: temperatura > 35°C por mais de 12 horas
        if (sensorData.Temperature > 35)
        {
            var twelveHoursAgo = DateTime.UtcNow.AddHours(-12);

            var highTempReadings = await _context.SensorReadings
                .Where(r => r.PlotId == sensorData.PlotId
                    && r.Timestamp >= twelveHoursAgo
                    && r.Temperature > 35)
                .CountAsync();

            var totalReadings = await _context.SensorReadings
                .Where(r => r.PlotId == sensorData.PlotId
                    && r.Timestamp >= twelveHoursAgo)
                .CountAsync();

            if (totalReadings >= 5 && highTempReadings >= (totalReadings * 0.8))
            {
                var existingAlert = await _context.Alerts
                    .FirstOrDefaultAsync(a => a.PlotId == sensorData.PlotId
                        && a.AlertType == "Calor Excessivo"
                        && !a.IsResolved);

                if (existingAlert == null)
                {
                    var alert = new Alert
                    {
                        Id = Guid.NewGuid(),
                        PlotId = sensorData.PlotId,
                        AlertType = "Calor Excessivo",
                        Message = $"Alerta de Calor: Temperatura acima de 35°C há mais de 12 horas. Valor atual: {sensorData.Temperature:F2}°C",
                        Severity = "Warning",
                        CreatedAt = DateTime.UtcNow,
                        IsResolved = false
                    };

                    _context.Alerts.Add(alert);
                    await _context.SaveChangesAsync();

                    await _publishEndpoint.Publish(new AlertCreated
                    {
                        Id = alert.Id,
                        PlotId = alert.PlotId,
                        AlertType = alert.AlertType,
                        Message = alert.Message,
                        CreatedAt = alert.CreatedAt,
                        Severity = alert.Severity
                    });

                    _logger.LogWarning("ALERTA DE CALOR criado para PlotId: {PlotId}", sensorData.PlotId);
                }
            }
        }
    }

    private async Task CheckPestAlert(SensorDataReceived sensorData)
    {
        // Regra: umidade > 80% + temperatura entre 20-30°C (condições favoráveis para pragas)
        if (sensorData.SoilMoisture > 80 && sensorData.Temperature >= 20 && sensorData.Temperature <= 30)
        {
            var existingAlert = await _context.Alerts
                .Where(a => a.PlotId == sensorData.PlotId
                    && a.AlertType == "Risco de Praga"
                    && !a.IsResolved
                    && a.CreatedAt >= DateTime.UtcNow.AddHours(-48))
                .FirstOrDefaultAsync();

            if (existingAlert == null)
            {
                var alert = new Alert
                {
                    Id = Guid.NewGuid(),
                    PlotId = sensorData.PlotId,
                    AlertType = "Risco de Praga",
                    Message = $"Condições favoráveis para pragas detectadas: Umidade {sensorData.SoilMoisture:F2}% e Temperatura {sensorData.Temperature:F2}°C",
                    Severity = "Warning",
                    CreatedAt = DateTime.UtcNow,
                    IsResolved = false
                };

                _context.Alerts.Add(alert);
                await _context.SaveChangesAsync();

                await _publishEndpoint.Publish(new AlertCreated
                {
                    Id = alert.Id,
                    PlotId = alert.PlotId,
                    AlertType = alert.AlertType,
                    Message = alert.Message,
                    CreatedAt = alert.CreatedAt,
                    Severity = alert.Severity
                });

                _logger.LogWarning("ALERTA DE RISCO DE PRAGA criado para PlotId: {PlotId}", sensorData.PlotId);
            }
        }
    }
}