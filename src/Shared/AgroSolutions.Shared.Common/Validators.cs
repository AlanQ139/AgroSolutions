namespace AgroSolutions.Shared.Common.Validators;

/// <summary>
/// Validador para dados de sensor
/// </summary>
public static class SensorDataValidator
{
    public static (bool IsValid, List<string> Errors) ValidateSensorData(
        decimal soilMoisture,
        decimal temperature,
        decimal precipitation)
    {
        var errors = new List<string>();

        // Validar umidade do solo (0-100%)
        if (soilMoisture < 0 || soilMoisture > 100)
        {
            errors.Add("A umidade do solo deve estar entre 0 e 100%");
        }

        // Validar temperatura (-50°C a 70°C)
        if (temperature < -50 || temperature > 70)
        {
            errors.Add("A temperatura deve estar entre -50°C e 70°C");
        }

        // Validar precipitação (não pode ser negativa)
        if (precipitation < 0)
        {
            errors.Add("A precipitação não pode ser negativa");
        }

        // Validar precipitação extrema (> 300mm é suspeito)
        if (precipitation > 300)
        {
            errors.Add("Valor de precipitação muito alto (> 300mm). Verifique o sensor.");
        }

        return (errors.Count == 0, errors);
    }

    public static bool IsValidPlotId(Guid plotId)
    {
        return plotId != Guid.Empty;
    }

    public static bool IsRealisticTemperature(decimal temperature)
    {
        // Temperaturas realistas para agricultura no Brasil: -10°C a 50°C
        return temperature >= -10 && temperature <= 50;
    }

    public static bool IsRealisticSoilMoisture(decimal soilMoisture)
    {
        // Umidade do solo realista: 5% a 100%
        return soilMoisture >= 5 && soilMoisture <= 100;
    }
}

/// <summary>
/// Validador para propriedades e talhões
/// </summary>
public static class PropertyValidator
{
    public static (bool IsValid, List<string> Errors) ValidateProperty(
        string name,
        decimal totalArea)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("O nome da propriedade é obrigatório");
        }
        else if (name.Length > 200)
        {
            errors.Add("O nome da propriedade deve ter no máximo 200 caracteres");
        }

        if (totalArea <= 0)
        {
            errors.Add("A área total deve ser maior que zero");
        }

        if (totalArea > 1000000) // 1 milhão de hectares
        {
            errors.Add("Área muito grande. Verifique o valor informado.");
        }

        return (errors.Count == 0, errors);
    }

    public static (bool IsValid, List<string> Errors) ValidatePlot(
        string name,
        string cropType,
        decimal area,
        decimal propertyTotalArea)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("O nome do talhão é obrigatório");
        }
        else if (name.Length > 200)
        {
            errors.Add("O nome do talhão deve ter no máximo 200 caracteres");
        }

        if (string.IsNullOrWhiteSpace(cropType))
        {
            errors.Add("O tipo de cultura é obrigatório");
        }

        if (area <= 0)
        {
            errors.Add("A área do talhão deve ser maior que zero");
        }

        if (area > propertyTotalArea)
        {
            errors.Add("A área do talhão não pode ser maior que a área total da propriedade");
        }

        return (errors.Count == 0, errors);
    }
}