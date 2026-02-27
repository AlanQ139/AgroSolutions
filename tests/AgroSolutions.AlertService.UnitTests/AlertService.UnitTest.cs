using AgroSolutions.Shared.Messages;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using System.Threading;
using System.Threading.Tasks;

namespace AgroSolutions.AlertService.UnitTests;

public class SensorDataConsumerTests
{
    private DbContextOptions<AgroSolutions.AlertService.AlertDbContext> CreateAlertDbOptions(string dbName)
        => new DbContextOptionsBuilder<AgroSolutions.AlertService.AlertDbContext>()
            .UseInMemoryDatabase(databaseName: dbName + "-alert")
            .Options;

    private DbContextOptions<AgroSolutions.AlertService.PropertyDbContext> CreatePropertyDbOptions(string dbName)
        => new DbContextOptionsBuilder<AgroSolutions.AlertService.PropertyDbContext>()
            .UseInMemoryDatabase(databaseName: dbName + "-property")
            .Options;

    // Low moisture immediate emergency (soil < 15) should create "Seca" alert
    [Fact]
    public async Task Consume_WithVeryLowSoilMoisture_CreatesDroughtAlert()
    {
        var alertDbOptions = CreateAlertDbOptions(nameof(Consume_WithVeryLowSoilMoisture_CreatesDroughtAlert));
        var propertyDbOptions = CreatePropertyDbOptions(nameof(Consume_WithVeryLowSoilMoisture_CreatesDroughtAlert));

        var publishMock = new Mock<IPublishEndpoint>();
        var loggerMock = new Mock<ILogger<AgroSolutions.AlertService.SensorDataConsumer>>();

        using var alertContext = new AgroSolutions.AlertService.AlertDbContext(alertDbOptions);
        using var propertyContext = new AgroSolutions.AlertService.PropertyDbContext(propertyDbOptions);

        var consumer = new AgroSolutions.AlertService.SensorDataConsumer(alertContext, propertyContext, publishMock.Object, loggerMock.Object);

        var message = new SensorDataReceived
        {
            Id = Guid.NewGuid(),
            PlotId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            SoilMoisture = 10m, // immediate emergency threshold (<15)
            Temperature = 25m,
            Precipitation = 0m
        };

        var ctxMock = new Mock<ConsumeContext<SensorDataReceived>>();
        ctxMock.SetupGet(c => c.Message).Returns(message);

        await consumer.Consume(ctxMock.Object);

        var alerts = await alertContext.Alerts.ToListAsync();
        alerts.Should().ContainSingle(a => a.AlertType == "Seca" && a.PlotId == message.PlotId);
        publishMock.Verify(p => p.Publish(It.IsAny<AlertCreated>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    // Temperature > 42 triggers immediate heat alert
    [Fact]
    public async Task Consume_WithVeryHighTemperature_CreatesHeatAlert()
    {
        var alertDbOptions = CreateAlertDbOptions(nameof(Consume_WithVeryHighTemperature_CreatesHeatAlert));
        var propertyDbOptions = CreatePropertyDbOptions(nameof(Consume_WithVeryHighTemperature_CreatesHeatAlert));

        var publishMock = new Mock<IPublishEndpoint>();
        var loggerMock = new Mock<ILogger<AgroSolutions.AlertService.SensorDataConsumer>>();

        using var alertContext = new AgroSolutions.AlertService.AlertDbContext(alertDbOptions);
        using var propertyContext = new AgroSolutions.AlertService.PropertyDbContext(propertyDbOptions);

        var consumer = new AgroSolutions.AlertService.SensorDataConsumer(alertContext, propertyContext, publishMock.Object, loggerMock.Object);

        var message = new SensorDataReceived
        {
            Id = Guid.NewGuid(),
            PlotId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            SoilMoisture = 50m,
            Temperature = 43m, // immediate emergency (>42)
            Precipitation = 0m
        };

        var ctxMock = new Mock<ConsumeContext<SensorDataReceived>>();
        ctxMock.SetupGet(c => c.Message).Returns(message);

        await consumer.Consume(ctxMock.Object);

        var alerts = await alertContext.Alerts.ToListAsync();
        alerts.Should().ContainSingle(a => a.AlertType == "Calor Excessivo" && a.PlotId == message.PlotId);
        publishMock.Verify(p => p.Publish(It.IsAny<AlertCreated>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    // Pest risk: SoilMoisture > 80 and Temperature between 20 and 30
    [Fact]
    public async Task Consume_WithPestConditions_CreatesPestAlert()
    {
        var alertDbOptions = CreateAlertDbOptions(nameof(Consume_WithPestConditions_CreatesPestAlert));
        var propertyDbOptions = CreatePropertyDbOptions(nameof(Consume_WithPestConditions_CreatesPestAlert));

        var publishMock = new Mock<IPublishEndpoint>();
        var loggerMock = new Mock<ILogger<AgroSolutions.AlertService.SensorDataConsumer>>();

        using var alertContext = new AgroSolutions.AlertService.AlertDbContext(alertDbOptions);
        using var propertyContext = new AgroSolutions.AlertService.PropertyDbContext(propertyDbOptions);

        var consumer = new AgroSolutions.AlertService.SensorDataConsumer(alertContext, propertyContext, publishMock.Object, loggerMock.Object);

        var message = new SensorDataReceived
        {
            Id = Guid.NewGuid(),
            PlotId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            SoilMoisture = 85m, // > 80
            Temperature = 25m,  // between 20 and 30
            Precipitation = 0m
        };

        var ctxMock = new Mock<ConsumeContext<SensorDataReceived>>();
        ctxMock.SetupGet(c => c.Message).Returns(message);

        await consumer.Consume(ctxMock.Object);

        var alerts = await alertContext.Alerts.ToListAsync();
        alerts.Should().ContainSingle(a => a.AlertType == "Risco de Praga" && a.PlotId == message.PlotId);
        publishMock.Verify(p => p.Publish(It.IsAny<AlertCreated>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}