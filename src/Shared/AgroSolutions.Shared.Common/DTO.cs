namespace AgroSolutions.Shared.Common.DTOs;

/// <summary>
/// DTO base para resposta de API
/// </summary>
public record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public List<string>? Errors { get; init; }

    public static ApiResponse<T> SuccessResponse(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }
}

/// <summary>
/// DTO para informações de propriedade
/// </summary>
public record PropertyDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public decimal TotalArea { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<PlotDto> Plots { get; init; } = new();
}

/// <summary>
/// DTO para informações de talhão
/// </summary>
public record PlotDto
{
    public Guid Id { get; init; }
    public Guid PropertyId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string CropType { get; init; } = string.Empty;
    public decimal Area { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO para dados de sensor
/// </summary>
public record SensorDataDto
{
    public Guid Id { get; init; }
    public Guid PlotId { get; init; }
    public DateTime Timestamp { get; init; }
    public decimal SoilMoisture { get; init; }
    public decimal Temperature { get; init; }
    public decimal Precipitation { get; init; }
}

/// <summary>
/// DTO para alerta
/// </summary>
public record AlertDto
{
    public Guid Id { get; init; }
    public Guid PlotId { get; init; }
    public string AlertType { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public bool IsResolved { get; init; }
}

/// <summary>
/// DTO para status de talhão
/// </summary>
public record PlotStatusDto
{
    public Guid PlotId { get; init; }
    public string PlotName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty; // Normal, Warning, Critical
    public List<string> ActiveAlerts { get; init; } = new();
    public SensorDataDto? LatestReading { get; init; }
    public PlotMetricsDto? Metrics { get; init; }
}

/// <summary>
/// DTO para métricas do talhão
/// </summary>
public record PlotMetricsDto
{
    public decimal AverageSoilMoisture { get; init; }
    public decimal AverageTemperature { get; init; }
    public decimal TotalPrecipitation { get; init; }
    public int TotalReadings { get; init; }
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
}

/// <summary>
/// DTO para usuário
/// </summary>
public record UserDto
{
    public string Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO para resposta de autenticação
/// </summary>
public record AuthResponseDto
{
    public string Token { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}

/// <summary>
/// DTO para dashboard
/// </summary>
public record DashboardDto
{
    public int TotalProperties { get; init; }
    public int TotalPlots { get; init; }
    public int ActiveAlerts { get; init; }
    public int TotalSensorReadings { get; init; }
    public List<PlotStatusDto> PlotStatuses { get; init; } = new();
    public List<AlertDto> RecentAlerts { get; init; } = new();
}