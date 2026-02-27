using AgroSolutions.PropertyService.Controllers;
using AgroSolutions.PropertyService.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace AgroSolutions.PropertyService.UnitTests;

public class PropertiesControllerTests
{
    private ApplicationDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    private PropertiesController CreateController(ApplicationDbContext ctx, string userId)
    {
        var loggerMock = new Mock<ILogger<PropertiesController>>();
        var controller = new PropertiesController(ctx, loggerMock.Object);

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        return controller;
    }

    [Fact]
    public async Task CreateProperty_WithValidRequest_ReturnsCreatedAndPersists()
    {
        var ctx = CreateInMemoryContext(nameof(CreateProperty_WithValidRequest_ReturnsCreatedAndPersists));
        var controller = CreateController(ctx, "user-123");

        var request = new CreatePropertyRequest("Fazenda A", "Sorocaba-SP", 120m);

        var result = await controller.CreateProperty(request);

        result.Should().BeOfType<CreatedAtActionResult>();

        var created = await ctx.Properties.FirstOrDefaultAsync();
        created.Should().NotBeNull();
        created!.Name.Should().Be("Fazenda A");
        created.UserId.Should().Be("user-123");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateProperty_WithEmptyName_ReturnsBadRequest(string name)
    {
        var ctx = CreateInMemoryContext(nameof(CreateProperty_WithEmptyName_ReturnsBadRequest));
        var controller = CreateController(ctx, "user-123");

        var request = new CreatePropertyRequest(name ?? string.Empty, "Sorocaba-SP", 100m);

        var result = await controller.CreateProperty(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}