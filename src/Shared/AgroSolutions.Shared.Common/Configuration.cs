namespace AgroSolutions.Shared.Common.Configuration;

/// <summary>
/// Configurações de JWT
/// </summary>
public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationInDays { get; set; } = 7;
}

/// <summary>
/// Configurações de RabbitMQ
/// </summary>
public class RabbitMqSettings
{
    public string Host { get; set; } = "localhost";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
}

/// <summary>
/// Configurações de alertas
/// </summary>
public class AlertSettings
{
    /// <summary>
    /// Limite de umidade do solo para alerta de seca (%)
    /// </summary>
    public decimal DroughtThreshold { get; set; } = 30m;

    /// <summary>
    /// Duração em horas para considerar alerta de seca
    /// </summary>
    public int DroughtDurationHours { get; set; } = 24;

    /// <summary>
    /// Limite de temperatura para alerta de calor (°C)
    /// </summary>
    public decimal HeatThreshold { get; set; } = 35m;

    /// <summary>
    /// Duração em horas para considerar alerta de calor
    /// </summary>
    public int HeatDurationHours { get; set; } = 12;

    /// <summary>
    /// Limite de umidade para alerta de risco de praga (%)
    /// </summary>
    public decimal PestMoistureThreshold { get; set; } = 80m;

    /// <summary>
    /// Temperatura mínima para risco de praga (°C)
    /// </summary>
    public decimal PestMinTemperature { get; set; } = 20m;

    /// <summary>
    /// Temperatura máxima para risco de praga (°C)
    /// </summary>
    public decimal PestMaxTemperature { get; set; } = 30m;

    /// <summary>
    /// Limite de umidade para resolver alerta de seca (%)
    /// </summary>
    public decimal DroughtResolutionThreshold { get; set; } = 35m;

    /// <summary>
    /// Percentual de leituras necessárias para gerar alerta (0-1)
    /// </summary>
    public decimal AlertTriggerPercentage { get; set; } = 0.8m;
}

/// <summary>
/// Configurações de database
/// </summary>
public class DatabaseSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public int CommandTimeout { get; set; } = 30;
    public bool EnableSensitiveDataLogging { get; set; } = false;
    public int MaxRetryCount { get; set; } = 3;
}

/// <summary>
/// Configurações de API
/// </summary>
public class ApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public int RequestTimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public bool EnableCors { get; set; } = true;
    public List<string> AllowedOrigins { get; set; } = new();
}

/// <summary>
/// Configurações de observabilidade
/// </summary>
public class ObservabilitySettings
{
    public bool EnableMetrics { get; set; } = true;
    public bool EnableTracing { get; set; } = true;
    public bool EnableLogging { get; set; } = true;
    public string ServiceName { get; set; } = string.Empty;
}