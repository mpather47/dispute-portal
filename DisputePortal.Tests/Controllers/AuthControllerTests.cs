using DisputePortal.Api.Controllers;
using DisputePortal.Api.DTOs;
using DisputePortal.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DisputePortal.Tests.Controllers;

public class AuthControllerTests
{
    private static (AuthController controller, Mock<IAuthService> serviceMock) Create()
    {
        var mock = new Mock<IAuthService>();
        return (new AuthController(mock.Object), mock);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var (controller, mock) = Create();
        var response = new LoginResponse("jwt-token", "u1", "Alice", "alice@test.com", "Customer");
        mock.Setup(s => s.LoginAsync(It.IsAny<LoginRequest>())).ReturnsAsync(response);

        var result = await controller.Login(new LoginRequest("alice@test.com", "pass"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<LoginResponse>(ok.Value);
        Assert.Equal("jwt-token", body.Token);
        Assert.Equal("Customer", body.Role);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        var (controller, mock) = Create();
        mock.Setup(s => s.LoginAsync(It.IsAny<LoginRequest>())).ReturnsAsync((LoginResponse?)null);

        var result = await controller.Login(new LoginRequest("bad@test.com", "wrong"));

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_PassesRequestToService()
    {
        var (controller, mock) = Create();
        mock.Setup(s => s.LoginAsync(It.IsAny<LoginRequest>())).ReturnsAsync((LoginResponse?)null);
        var request = new LoginRequest("user@test.com", "Password123!");

        await controller.Login(request);

        mock.Verify(s => s.LoginAsync(It.Is<LoginRequest>(
            r => r.Email == "user@test.com" && r.Password == "Password123!")), Times.Once);
    }
}
