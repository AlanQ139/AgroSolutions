using AgroSolutions.Shared.Messages;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace AgroSolutions.SensorIngestionService.Controllers;

public record SensorDataRequest(
    Guid PlotId,
    decimal SoilMoisture,
    decimal Temperature,
    decimal Precipitation
);

[ApiController]
[Route("api/[controller]")]
public class SensorsController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<SensorsController> _logger;

    public SensorsController(IPublishEndpoint publishEndpoint, ILogger<SensorsController> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    [HttpPost("ingest")]
    public async Task<IActionResult> IngestSensorData([FromBody] SensorDataRequest request)
    {
        if (request.SoilMoisture < 0 || request.SoilMoisture > 100)
        {
            return BadRequest("Umidade do solo deve estar entre 0 e 100%");
        }

        if (request.Temperature < -50 || request.Temperature > 70)
        {
            return BadRequest("Temperatura deve estar entre -50 e 70°C");
        }

        if (request.Precipitation < 0)
        {
            return BadRequest("Precipitação não pode ser negativa");
        }

        var sensorEvent = new SensorDataReceived
        {
            Id = Guid.NewGuid(),
            PlotId = request.PlotId,
            Timestamp = DateTime.UtcNow,
            SoilMoisture = request.SoilMoisture,
            Temperature = request.Temperature,
            Precipitation = request.Precipitation
        };

        await _publishEndpoint.Publish(sensorEvent);

        _logger.LogInformation(
            "Dados de sensor recebidos e publicados - PlotId: {PlotId}, Umidade: {Moisture}%, Temp: {Temp}°C",
            request.PlotId, request.SoilMoisture, request.Temperature);

        return Accepted(new
        {
            message = "Dados recebidos e enviados para processamento",
            eventId = sensorEvent.Id,
            timestamp = sensorEvent.Timestamp
        });
    }

    [HttpPost("ingest/batch")]
    public async Task<IActionResult> IngestBatchSensorData([FromBody] List<SensorDataRequest> requests)
    {
        if (!requests.Any())
        {
            return BadRequest("Lista de dados não pode estar vazia");
        }

        var events = new List<SensorDataReceived>();

        foreach (var request in requests)
        {
            var sensorEvent = new SensorDataReceived
            {
                Id = Guid.NewGuid(),
                PlotId = request.PlotId,
                Timestamp = DateTime.UtcNow,
                SoilMoisture = request.SoilMoisture,
                Temperature = request.Temperature,
                Precipitation = request.Precipitation
            };

            events.Add(sensorEvent);
            await _publishEndpoint.Publish(sensorEvent);
        }

        _logger.LogInformation("Batch de {Count} leituras de sensores publicado", events.Count);

        return Accepted(new
        {
            message = $"{events.Count} leituras recebidas e enviadas para processamento",
            count = events.Count
        });
    }

    [HttpGet("simulate/{plotId}")]
    public async Task<IActionResult> SimulateSensorData(Guid plotId)
    {
        var random = new Random();

        var sensorEvent = new SensorDataReceived
        {
            Id = Guid.NewGuid(),
            PlotId = plotId,
            Timestamp = DateTime.UtcNow,
            SoilMoisture = (decimal)(random.NextDouble() * 100), // 0-100%
            Temperature = (decimal)(random.NextDouble() * 40), // 0-40°C
            Precipitation = (decimal)(random.NextDouble() * 10) // 0-10mm
        };

        await _publishEndpoint.Publish(sensorEvent);

        _logger.LogInformation("Dados simulados gerados para PlotId: {PlotId}", plotId);

        return Ok(new
        {
            message = "Dados simulados gerados",
            data = sensorEvent
        });
    }
}