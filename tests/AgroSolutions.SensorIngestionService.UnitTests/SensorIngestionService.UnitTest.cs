using AgroSolutions.SensorIngestionService.Controllers;
using AgroSolutions.Shared.Messages;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace AgroSolutions.SensorIngestionService.UnitTests;

public class SensorsControllerTests
{
    private SensorsController CreateController(Mock<IPublishEndpoint> publishMock, out Mock<ILogger<SensorsController>> loggerMock)
    {
        loggerMock = new Mock<ILogger<SensorsController>>();
        var controller = new SensorsController(publishMock.Object, loggerMock.Object);
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    [Fact]
    public async Task IngestSensorData_WithValidPayload_ReturnsAcceptedAndPublishes()
    {
        var publishMock = new Mock<IPublishEndpoint>();
        var controller = CreateController(publishMock, out _);

        var request = new AgroSolutions.SensorIngestionService.Controllers.SensorDataRequest(
            PlotId: Guid.NewGuid(),
            SoilMoisture: 55m,
            Temperature: 28m,
            Precipitation: 2m
        );

        var result = await controller.IngestSensorData(request);

        result.Should().BeOfType<AcceptedResult>();
        publishMock.Verify(p => p.Publish(It.IsAny<SensorDataReceived>(), default), Times.Once);
    }

    [Theory]
    [InlineData(-1, 25, 0)]
    [InlineData(101, 25, 0)]
    public async Task IngestSensorData_WithInvalidSoilMoisture_ReturnsBadRequest(decimal moisture, decimal temp, decimal precip)
    {
        var publishMock = new Mock<IPublishEndpoint>();
        var controller = CreateController(publishMock, out _);

        var request = new AgroSolutions.SensorIngestionService.Controllers.SensorDataRequest(
            PlotId: Guid.NewGuid(),
            SoilMoisture: moisture,
            Temperature: temp,
            Precipitation: precip
        );

        var result = await controller.IngestSensorData(request);

        result.Should().BeOfType<BadRequestObjectResult>();
        publishMock.Verify(p => p.Publish(It.IsAny<SensorDataReceived>(), default), Times.Never);
    }

    [Fact]
    public async Task IngestBatchSensorData_WithEmptyList_ReturnsBadRequest()
    {
        var publishMock = new Mock<IPublishEndpoint>();
        var controller = CreateController(publishMock, out _);

        var result = await controller.IngestBatchSensorData(new List<AgroSolutions.SensorIngestionService.Controllers.SensorDataRequest>());

        result.Should().BeOfType<BadRequestObjectResult>();
        publishMock.Verify(p => p.Publish(It.IsAny<SensorDataReceived>(), default), Times.Never);
    }
}