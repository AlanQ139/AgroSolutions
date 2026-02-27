using AgroSolutions.IdentityService.Controllers;
using AgroSolutions.IdentityService.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace AgroSolutions.IdentityService.UnitTests;

public class AuthControllerTests
{
    private Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null, null, null, null, null, null, null, null);
    }

    private IConfiguration CreateConfiguration()
    {
        var dict = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "AgroSolutions-Test-Secret-ChangeInProd-0123456789",
            ["Jwt:Issuer"] = "AgroSolutions",
            ["Jwt:Audience"] = "AgroSolutions"
        };
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [Fact]
    public async Task Login_WhenUserNotFound_ReturnsUnauthorized()
    {
        var userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(u => u.FindByEmailAsync(It.IsAny<string>()))
                       .ReturnsAsync((ApplicationUser?)null);

        var config = CreateConfiguration();
        var loggerMock = new Mock<ILogger<AuthController>>();
        var controller = new AuthController(userManagerMock.Object, config, loggerMock.Object);

        var result = await controller.Login(new LoginRequest("no@one.com", "pass"));

        // UnauthorizedObjectResult expected when user not found
        result.Should().BeOfType<Microsoft.AspNetCore.Mvc.UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WhenPasswordInvalid_ReturnsUnauthorized()
    {
        var user = new ApplicationUser { Id = "u1", Email = "a@b.com", UserName = "a@b.com", FullName = "Test" };
        var userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(u => u.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        userManagerMock.Setup(u => u.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);

        var config = CreateConfiguration();
        var loggerMock = new Mock<ILogger<AuthController>>();
        var controller = new AuthController(userManagerMock.Object, config, loggerMock.Object);

        var result = await controller.Login(new LoginRequest(user.Email, "wrong"));

        result.Should().BeOfType<Microsoft.AspNetCore.Mvc.UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithToken()
    {
        var user = new ApplicationUser { Id = "u1", Email = "a@b.com", UserName = "a@b.com", FullName = "Test" };
        var userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(u => u.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        userManagerMock.Setup(u => u.CheckPasswordAsync(user, "correct")).ReturnsAsync(true);

        var config = CreateConfiguration();
        var loggerMock = new Mock<ILogger<AuthController>>();
        var controller = new AuthController(userManagerMock.Object, config, loggerMock.Object);

        var result = await controller.Login(new LoginRequest(user.Email, "correct"));

        result.Should().BeOfType<Microsoft.AspNetCore.Mvc.OkObjectResult>();
        var ok = result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        ok!.Value.Should().NotBeNull();
    }
}