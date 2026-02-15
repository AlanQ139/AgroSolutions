using AgroSolutions.PropertyService.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AgroSolutions.PropertyService.Controllers;

public record CreatePropertyRequest(string Name, string Location, decimal TotalArea);
public record CreatePlotRequest(string Name, string CropType, decimal Area);
public record SensorDataResponse(Guid Id, DateTime Timestamp, decimal SoilMoisture, decimal Temperature, decimal Precipitation);
public record AlertResponse(Guid Id, string AlertType, string Message, string Severity, DateTime CreatedAt, bool IsResolved);
public record PlotStatusResponse(Guid PlotId, string PlotName, string Status, List<string> ActiveAlerts);

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PropertiesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PropertiesController> _logger;

    public PropertiesController(ApplicationDbContext context, ILogger<PropertiesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<IActionResult> GetProperties()
    {
        var userId = GetUserId();
        var properties = await _context.Properties
            .Include(p => p.Plots)
            .Where(p => p.UserId == userId)
            .ToListAsync();

        return Ok(properties);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProperty([FromBody] CreatePropertyRequest request)
    {
        var userId = GetUserId();

        var property = new Property
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            Location = request.Location,
            TotalArea = request.TotalArea,
            CreatedAt = DateTime.UtcNow
        };

        _context.Properties.Add(property);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Nova propriedade cadastrada: {PropertyId} - {Name}", property.Id, property.Name);

        return CreatedAtAction(nameof(GetProperty), new { id = property.Id }, property);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProperty(Guid id)
    {
        var userId = GetUserId();
        var property = await _context.Properties
            .Include(p => p.Plots)
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (property == null)
            return NotFound();

        return Ok(property);
    }
}

[ApiController]
[Route("api/properties/{propertyId}/[controller]")]
[Authorize]
public class PlotsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PlotsController> _logger;

    public PlotsController(ApplicationDbContext context, ILogger<PlotsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<IActionResult> GetPlots(Guid propertyId)
    {
        var userId = GetUserId();

        var property = await _context.Properties
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.UserId == userId);

        if (property == null)
            return NotFound("Propriedade não encontrada");

        var plots = await _context.Plots
            .Where(p => p.PropertyId == propertyId)
            .ToListAsync();

        return Ok(plots);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePlot(Guid propertyId, [FromBody] CreatePlotRequest request)
    {
        var userId = GetUserId();

        var property = await _context.Properties
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.UserId == userId);

        if (property == null)
            return NotFound("Propriedade não encontrada");

        var plot = new Plot
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            Name = request.Name,
            CropType = request.CropType,
            Area = request.Area,
            CreatedAt = DateTime.UtcNow
        };

        _context.Plots.Add(plot);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Novo talhão cadastrado: {PlotId} - {Name}", plot.Id, plot.Name);

        return CreatedAtAction(nameof(GetPlot), new { propertyId, id = plot.Id }, plot);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPlot(Guid propertyId, Guid id)
    {
        var userId = GetUserId();

        var plot = await _context.Plots
            .Include(p => p.Property)
            .FirstOrDefaultAsync(p => p.Id == id && p.PropertyId == propertyId && p.Property.UserId == userId);

        if (plot == null)
            return NotFound();

        return Ok(plot);
    }

    [HttpGet("{id}/sensor-data")]
    public async Task<IActionResult> GetSensorData(Guid propertyId, Guid id, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var userId = GetUserId();

        var plot = await _context.Plots
            .Include(p => p.Property)
            .FirstOrDefaultAsync(p => p.Id == id && p.PropertyId == propertyId && p.Property.UserId == userId);

        if (plot == null)
            return NotFound();

        var query = _context.SensorData.Where(s => s.PlotId == id);

        if (startDate.HasValue)
            query = query.Where(s => s.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(s => s.Timestamp <= endDate.Value);

        var data = await query
            .OrderByDescending(s => s.Timestamp)
            .Take(1000)
            .Select(s => new SensorDataResponse(s.Id, s.Timestamp, s.SoilMoisture, s.Temperature, s.Precipitation))
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("{id}/alerts")]
    public async Task<IActionResult> GetAlerts(Guid propertyId, Guid id, [FromQuery] bool? onlyActive)
    {
        var userId = GetUserId();

        var plot = await _context.Plots
            .Include(p => p.Property)
            .FirstOrDefaultAsync(p => p.Id == id && p.PropertyId == propertyId && p.Property.UserId == userId);

        if (plot == null)
            return NotFound();

        var query = _context.Alerts.Where(a => a.PlotId == id);

        if (onlyActive == true)
            query = query.Where(a => !a.IsResolved);

        var alerts = await query
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AlertResponse(a.Id, a.AlertType, a.Message, a.Severity, a.CreatedAt, a.IsResolved))
            .ToListAsync();

        return Ok(alerts);
    }

    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetPlotStatus(Guid propertyId, Guid id)
    {
        var userId = GetUserId();

        var plot = await _context.Plots
            .Include(p => p.Property)
            .FirstOrDefaultAsync(p => p.Id == id && p.PropertyId == propertyId && p.Property.UserId == userId);

        if (plot == null)
            return NotFound();

        var activeAlerts = await _context.Alerts
            .Where(a => a.PlotId == id && !a.IsResolved)
            .Select(a => a.AlertType)
            .ToListAsync();

        var status = activeAlerts.Any(a => a.Contains("Crítico")) ? "Crítico" :
                     activeAlerts.Any() ? "Alerta" : "Normal";

        var response = new PlotStatusResponse(
            plot.Id,
            plot.Name,
            status,
            activeAlerts
        );

        return Ok(response);
    }
}