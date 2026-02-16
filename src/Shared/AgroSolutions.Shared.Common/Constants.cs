namespace AgroSolutions.Shared.Common.Constants;

/// <summary>
/// Constantes de tipos de alerta
/// </summary>
public static class AlertTypes
{
    public const string Drought = "Seca";
    public const string ExcessiveHeat = "Calor Excessivo";
    public const string PestRisk = "Risco de Praga";
    public const string ExcessiveMoisture = "Umidade Excessiva";
    public const string Frost = "Risco de Geada";
    public const string StrongWind = "Vento Forte";
}

/// <summary>
/// Constantes de severidade de alerta
/// </summary>
public static class AlertSeverity
{
    public const string Info = "Info";
    public const string Warning = "Warning";
    public const string Critical = "Critical";
}

/// <summary>
/// Constantes de status de talhão
/// </summary>
public static class PlotStatus
{
    public const string Normal = "Normal";
    public const string Warning = "Alerta";
    public const string Critical = "Crítico";
    public const string Unknown = "Desconhecido";
}

/// <summary>
/// Constantes de tipos de cultura
/// </summary>
public static class CropTypes
{
    public const string Soy = "Soja";
    public const string Corn = "Milho";
    public const string Cotton = "Algodão";
    public const string SugarCane = "Cana-de-Açúcar";
    public const string Coffee = "Café";
    public const string Rice = "Arroz";
    public const string Beans = "Feijão";
    public const string Wheat = "Trigo";
    public const string Other = "Outros";
}

/// <summary>
/// Enumeração de severidade de alerta
/// </summary>
public enum AlertSeverityEnum
{
    Info = 0,
    Warning = 1,
    Critical = 2
}

/// <summary>
/// Enumeração de status de talhão
/// </summary>
public enum PlotStatusEnum
{
    Unknown = 0,
    Normal = 1,
    Warning = 2,
    Critical = 3
}

/// <summary>
/// Enumeração de tipos de sensor
/// </summary>
public enum SensorType
{
    SoilMoisture = 1,
    Temperature = 2,
    Precipitation = 3,
    Humidity = 4,
    WindSpeed = 5,
    SolarRadiation = 6
}