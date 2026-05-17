using System.Security.Claims;
using DisputePortal.Api.Controllers;
using DisputePortal.Api.DTOs;
using DisputePortal.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DisputePortal.Tests.Controllers;

public class DisputesControllerTests
{
    private static DisputeResponse MakeDispute(int id = 1) => new(
        id, $"CASE-00{id}", 10, "Merchant", 99.99m,
        "Fraud", "Notes", null, "Submitted",
        DateTime.UtcNow, null, []);

    private static (DisputesController controller, Mock<IDisputeService> mock) Create(
        string userId = "user-1", string role = "Customer")
    {
        var mock = new Mock<IDisputeService>();
        var controller = new DisputesController(mock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Name, "Test User"),
                    new Claim(ClaimTypes.Role, role),
                ], "Test"))
            }
        };
        return (controller, mock);
    }

    [Fact]
    public async Task CreateDispute_ValidRequest_Returns201WithLocation()
    {
        var (controller, mock) = Create();
        var dispute = MakeDispute(5);
        mock.Setup(s => s.CreateDisputeAsync("user-1", It.IsAny<CreateDisputeRequest>()))
            .ReturnsAsync(dispute);

        var result = await controller.CreateDispute(
            new CreateDisputeRequest(10, "Fraud", "Details"));

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal(dispute, created.Value);
        Assert.Equal(5, created.RouteValues!["id"]);
    }

    [Fact]
    public async Task CreateDispute_PassesUserIdToService()
    {
        var (controller, mock) = Create(userId: "user-42");
        mock.Setup(s => s.CreateDisputeAsync(It.IsAny<string>(), It.IsAny<CreateDisputeRequest>()))
            .ReturnsAsync(MakeDispute());

        await controller.CreateDispute(new CreateDisputeRequest(10, "Fraud", ""));

        mock.Verify(s => s.CreateDisputeAsync("user-42", It.IsAny<CreateDisputeRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetMyDisputes_Returns200WithList()
    {
        var (controller, mock) = Create();
        var disputes = new List<DisputeResponse> { MakeDispute(1), MakeDispute(2) };
        mock.Setup(s => s.GetCustomerDisputesAsync("user-1")).ReturnsAsync(disputes);

        var result = await controller.GetMyDisputes();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<List<DisputeResponse>>(ok.Value);
        Assert.Equal(2, body.Count);
    }

    [Fact]
    public async Task GetDisputeById_CustomerOwnsDispute_Returns200()
    {
        var (controller, mock) = Create(userId: "user-1", role: "Customer");
        var dispute = MakeDispute(3);
        mock.Setup(s => s.GetDisputeByIdAsync(3, "user-1", false)).ReturnsAsync(dispute);

        var result = await controller.GetDisputeById(3);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(dispute, ok.Value);
    }

    [Fact]
    public async Task GetDisputeById_AdminRole_PassesIsAdminTrue()
    {
        var (controller, mock) = Create(userId: "admin-1", role: "Admin");
        mock.Setup(s => s.GetDisputeByIdAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(MakeDispute());

        await controller.GetDisputeById(1);

        mock.Verify(s => s.GetDisputeByIdAsync(1, "admin-1", true), Times.Once);
    }
}
