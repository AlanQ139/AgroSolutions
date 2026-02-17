using AgroSolutions.PropertyService.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AgroSolutions.PropertyService.Controllers;

// -------------------------------------------------------
// REQUEST / RESPONSE RECORDS
// -------------------------------------------------------

public record CreatePropertyRequest(
    string Name,
    string Location,
    decimal TotalArea
);

public record CreatePlotRequest(
    string Name,
    string CropType,
    decimal Area
);

// -------------------------------------------------------
// PROPERTIES CONTROLLER
// -------------------------------------------------------

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

    // FIX: o claim correto para o ID do usuário via ASP.NET Identity + JWT é
    // ClaimTypes.NameIdentifier ou "sub". O código anterior usava o ClaimTypes errado.
    private string GetUserId()
    {
        // "sub" é o claim padrão gerado pelo JwtRegisteredClaimNames.Sub no IdentityService
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub")
                  ?? User.FindFirstValue("id");

        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("User ID not found in token claims.");

        return userId;
    }

    // GET api/properties
    [HttpGet]
    public async Task<IActionResult> GetProperties()
    {
        try
        {
            var userId = GetUserId();

            var properties = await _context.Properties
                .Include(p => p.Plots)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.Id,
                    p.UserId,
                    p.Name,
                    p.Location,
                    p.TotalArea,
                    p.CreatedAt,
                    Plots = p.Plots.Select(pl => new
                    {
                        pl.Id,
                        pl.PropertyId,
                        pl.Name,
                        pl.CropType,
                        pl.Area,
                        pl.CreatedAt
                    }).ToList()
                })
                .ToListAsync();

            return Ok(properties);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized access: {Message}", ex.Message);
            return Unauthorized(new { message = "Token inválido ou expirado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching properties");
            return StatusCode(500, new { message = "Erro interno ao buscar propriedades", detail = ex.Message });
        }
    }

    // POST api/properties
    [HttpPost]
    public async Task<IActionResult> CreateProperty([FromBody] CreatePropertyRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Nome é obrigatório" });

            if (request.TotalArea <= 0)
                return BadRequest(new { message = "Área total deve ser maior que zero" });

            var userId = GetUserId();

            var property = new Property
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = request.Name.Trim(),
                Location = request.Location?.Trim() ?? string.Empty,
                TotalArea = request.TotalArea,
                CreatedAt = DateTime.UtcNow
            };

            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Property created: {Id} - {Name} for user {UserId}",
                property.Id, property.Name, userId);

            var result = new
            {
                property.Id,
                property.UserId,
                property.Name,
                property.Location,
                property.TotalArea,
                property.CreatedAt,
                Plots = new List<object>()
            };

            return CreatedAtAction(nameof(GetProperty), new { id = property.Id }, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized: {Message}", ex.Message);
            return Unauthorized(new { message = "Token inválido ou expirado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating property");
            return StatusCode(500, new { message = "Erro ao criar propriedade", detail = ex.Message });
        }
    }

    // GET api/properties/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProperty(Guid id)
    {
        try
        {
            var userId = GetUserId();

            var property = await _context.Properties
                .Include(p => p.Plots)
                .Where(p => p.Id == id && p.UserId == userId)
                .Select(p => new
                {
                    p.Id,
                    p.UserId,
                    p.Name,
                    p.Location,
                    p.TotalArea,
                    p.CreatedAt,
                    Plots = p.Plots.Select(pl => new
                    {
                        pl.Id,
                        pl.PropertyId,
                        pl.Name,
                        pl.CropType,
                        pl.Area,
                        pl.CreatedAt
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (property == null)
                return NotFound(new { message = "Propriedade não encontrada" });

            return Ok(property);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Token inválido ou expirado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching property {Id}", id);
            return StatusCode(500, new { message = "Erro ao buscar propriedade", detail = ex.Message });
        }
    }

    // DELETE api/properties/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProperty(Guid id)
    {
        try
        {
            var userId = GetUserId();

            var property = await _context.Properties
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (property == null)
                return NotFound(new { message = "Propriedade não encontrada" });

            _context.Properties.Remove(property);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Property deleted: {Id}", id);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Token inválido ou expirado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting property {Id}", id);
            return StatusCode(500, new { message = "Erro ao deletar propriedade", detail = ex.Message });
        }
    }
}

// -------------------------------------------------------
// PLOTS CONTROLLER
// -------------------------------------------------------

[ApiController]
[Route("api/properties/{propertyId:guid}/plots")]
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

    private string GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub")
                  ?? User.FindFirstValue("id");

        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("User ID not found in token claims.");

        return userId;
    }

    // Verifica se a propriedade existe e pertence ao usuário
    private async Task<Property?> GetOwnedPropertyAsync(Guid propertyId, string userId)
    {
        return await _context.Properties
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.UserId == userId);
    }

    // GET api/properties/{propertyId}/plots
    [HttpGet]
    public async Task<IActionResult> GetPlots(Guid propertyId)
    {
        try
        {
            var userId = GetUserId();

            var property = await GetOwnedPropertyAsync(propertyId, userId);
            if (property == null)
                return NotFound(new { message = "Propriedade não encontrada" });

            var plots = await _context.Plots
                .Where(p => p.PropertyId == propertyId)
                .OrderBy(p => p.Name)
                .Select(p => new
                {
                    p.Id,
                    p.PropertyId,
                    p.Name,
                    p.CropType,
                    p.Area,
                    p.CreatedAt
                })
                .ToListAsync();

            return Ok(plots);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Token inválido ou expirado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching plots for property {PropertyId}", propertyId);
            return StatusCode(500, new { message = "Erro ao buscar talhões", detail = ex.Message });
        }
    }

    // POST api/properties/{propertyId}/plots
    [HttpPost]
    public async Task<IActionResult> CreatePlot(Guid propertyId, [FromBody] CreatePlotRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Nome do talhão é obrigatório" });

            if (string.IsNullOrWhiteSpace(request.CropType))
                return BadRequest(new { message = "Tipo de cultura é obrigatório" });

            if (request.Area <= 0)
                return BadRequest(new { message = "Área deve ser maior que zero" });

            var userId = GetUserId();

            var property = await GetOwnedPropertyAsync(propertyId, userId);
            if (property == null)
                return NotFound(new { message = "Propriedade não encontrada" });

            if (request.Area > property.TotalArea)
                return BadRequest(new
                {
                    message = $"Área do talhão ({request.Area} ha) não pode ser maior que a área total da propriedade ({property.TotalArea} ha)"
                });

            var plot = new Plot
            {
                Id = Guid.NewGuid(),
                PropertyId = propertyId,
                Name = request.Name.Trim(),
                CropType = request.CropType.Trim(),
                Area = request.Area,
                CreatedAt = DateTime.UtcNow
            };

            _context.Plots.Add(plot);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Plot created: {Id} - {Name} on property {PropertyId}",
                plot.Id, plot.Name, propertyId);

            var result = new
            {
                plot.Id,
                plot.PropertyId,
                plot.Name,
                plot.CropType,
                plot.Area,
                plot.CreatedAt
            };

            return CreatedAtAction(nameof(GetPlot), new { propertyId, id = plot.Id }, result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Token inválido ou expirado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating plot for property {PropertyId}", propertyId);
            return StatusCode(500, new { message = "Erro ao criar talhão", detail = ex.Message });
        }
    }

    // GET api/properties/{propertyId}/plots/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPlot(Guid propertyId, Guid id)
    {
        try
        {
            var userId = GetUserId();

            var property = await GetOwnedPropertyAsync(propertyId, userId);
            if (property == null)
                return NotFound(new { message = "Propriedade não encontrada" });

            var plot = await _context.Plots
                .Where(p => p.Id == id && p.PropertyId == propertyId)
                .Select(p => new
                {
                    p.Id,
                    p.PropertyId,
                    p.Name,
                    p.CropType,
                    p.Area,
                    p.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (plot == null)
                return NotFound(new { message = "Talhão não encontrado" });

            return Ok(plot);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Token inválido" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching plot {Id}", id);
            return StatusCode(500, new { message = "Erro ao buscar talhão", detail = ex.Message });
        }
    }

    // GET api/properties/{propertyId}/plots/{id}/sensor-data
    [HttpGet("{id:guid}/sensor-data")]
    public async Task<IActionResult> GetSensorData(
        Guid propertyId,
        Guid id,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        try
        {
            var userId = GetUserId();

            var property = await GetOwnedPropertyAsync(propertyId, userId);
            if (property == null)
                return NotFound(new { message = "Propriedade não encontrada" });

            var plotExists = await _context.Plots
                .AnyAsync(p => p.Id == id && p.PropertyId == propertyId);

            if (!plotExists)
                return NotFound(new { message = "Talhão não encontrado" });

            var query = _context.SensorReadings
                .Where(s => s.PlotId == id);

            if (startDate.HasValue)
                query = query.Where(s => s.Timestamp >= startDate.Value.ToUniversalTime());

            if (endDate.HasValue)
                query = query.Where(s => s.Timestamp <= endDate.Value.ToUniversalTime());

            var totalCount = await query.CountAsync();

            var data = await query
                .OrderByDescending(s => s.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    s.Id,
                    s.PlotId,
                    s.Timestamp,
                    s.SoilMoisture,
                    s.Temperature,
                    s.Precipitation
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Data = data
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Token inválido" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sensor data for plot {Id}", id);
            return StatusCode(500, new { message = "Erro ao buscar dados de sensor", detail = ex.Message });
        }
    }

    // GET api/properties/{propertyId}/plots/{id}/alerts
    [HttpGet("{id:guid}/alerts")]
    public async Task<IActionResult> GetAlerts(
        Guid propertyId,
        Guid id,
        [FromQuery] bool onlyActive = false)
    {
        try
        {
            var userId = GetUserId();

            var property = await GetOwnedPropertyAsync(propertyId, userId);
            if (property == null)
                return NotFound(new { message = "Propriedade não encontrada" });

            var plotExists = await _context.Plots
                .AnyAsync(p => p.Id == id && p.PropertyId == propertyId);

            if (!plotExists)
                return NotFound(new { message = "Talhão não encontrado" });

            var query = _context.Alerts.Where(a => a.PlotId == id);

            if (onlyActive)
                query = query.Where(a => !a.IsResolved);

            var alerts = await query
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    a.Id,
                    a.PlotId,
                    a.AlertType,
                    a.Message,
                    a.Severity,
                    a.CreatedAt,
                    a.IsResolved
                })
                .ToListAsync();

            return Ok(alerts);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Token inválido" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching alerts for plot {Id}", id);
            return StatusCode(500, new { message = "Erro ao buscar alertas", detail = ex.Message });
        }
    }

    // GET api/properties/{propertyId}/plots/{id}/status
    [HttpGet("{id:guid}/status")]
    public async Task<IActionResult> GetPlotStatus(Guid propertyId, Guid id)
    {
        try
        {
            var userId = GetUserId();

            var property = await GetOwnedPropertyAsync(propertyId, userId);
            if (property == null)
                return NotFound(new { message = "Propriedade não encontrada" });

            var plot = await _context.Plots
                .FirstOrDefaultAsync(p => p.Id == id && p.PropertyId == propertyId);

            if (plot == null)
                return NotFound(new { message = "Talhão não encontrado" });

            var activeAlerts = await _context.Alerts
                .Where(a => a.PlotId == id && !a.IsResolved)
                .Select(a => new { a.AlertType, a.Severity, a.Message, a.CreatedAt })
                .ToListAsync();

            var latestReading = await _context.SensorReadings
                .Where(s => s.PlotId == id)
                .OrderByDescending(s => s.Timestamp)
                .Select(s => new
                {
                    s.Timestamp,
                    s.SoilMoisture,
                    s.Temperature,
                    s.Precipitation
                })
                .FirstOrDefaultAsync();

            var status = activeAlerts.Any(a => a.Severity == "Critical") ? "Crítico" :
                         activeAlerts.Any() ? "Alerta" :
                         latestReading != null ? "Normal" : "Sem Dados";

            return Ok(new
            {
                PlotId = plot.Id,
                PlotName = plot.Name,
                CropType = plot.CropType,
                Status = status,
                ActiveAlerts = activeAlerts,
                LatestReading = latestReading,
                LastUpdated = latestReading?.Timestamp
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Token inválido" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching status for plot {Id}", id);
            return StatusCode(500, new { message = "Erro ao buscar status", detail = ex.Message });
        }
    }
}