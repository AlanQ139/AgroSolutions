namespace AgroSolutions.Shared.Messages;

/// <summary>
/// Evento publicado quando dados de sensor são recebidos
/// </summary>
public record SensorDataReceived
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid PlotId { get; init; }
    public DateTime Timestamp { get; init; }
    public decimal SoilMoisture { get; init; } // Umidade do solo (%)
    public decimal Temperature { get; init; } // Temperatura (°C)
    public decimal Precipitation { get; init; } // Precipitação (mm)
}

/// <summary>
/// Evento publicado quando um alerta é criado
/// </summary>
public record AlertCreated
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid PlotId { get; init; }
    public string AlertType { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string Severity { get; init; } = "Warning"; // Info, Warning, Critical
}