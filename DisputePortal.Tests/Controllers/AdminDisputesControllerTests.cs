using System.Security.Claims;
using DisputePortal.Api.Controllers;
using DisputePortal.Api.DTOs;
using DisputePortal.Api.Models;
using DisputePortal.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DisputePortal.Tests.Controllers;

public class AdminDisputesControllerTests
{
    private static DisputeResponse MakeDispute(int id = 1) => new(
        id, $"CASE-00{id}", 10, "Merchant", 99.99m,
        "Fraud", "Notes", null, "Submitted",
        DateTime.UtcNow, null, []);

    private static (AdminDisputesController controller, Mock<IDisputeService> mock) Create(
        string adminName = "Admin User")
    {
        var mock = new Mock<IDisputeService>();
        var controller = new AdminDisputesController(mock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "admin-1"),
                    new Claim(ClaimTypes.Name, adminName),
                    new Claim(ClaimTypes.Role, "Admin"),
                ], "Test"))
            }
        };
        return (controller, mock);
    }

    [Fact]
    public async Task GetAllDisputes_Returns200WithPagedResult()
    {
        var (controller, mock) = Create();
        var paged = new PagedResult<DisputeResponse>(
            [MakeDispute(1), MakeDispute(2)], 2, 1, 20);
        mock.Setup(s => s.GetAllDisputesForAdminAsync(1, 20)).ReturnsAsync(paged);

        var result = await controller.GetAllDisputes(1, 20);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<PagedResult<DisputeResponse>>(ok.Value);
        Assert.Equal(2, body.TotalCount);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(101, 100)]
    [InlineData(50, 50)]
    public async Task GetAllDisputes_ClampesPageSize(int requested, int expected)
    {
        var (controller, mock) = Create();
        mock.Setup(s => s.GetAllDisputesForAdminAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResult<DisputeResponse>([], 0, 1, expected));

        await controller.GetAllDisputes(1, requested);

        mock.Verify(s => s.GetAllDisputesForAdminAsync(1, expected), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_Returns200WithUpdatedDispute()
    {
        var (controller, mock) = Create();
        var updated = MakeDispute(3) with { Status = "UnderReview" };
        mock.Setup(s => s.UpdateDisputeStatusAsync(
            3, It.IsAny<UpdateDisputeStatusRequest>(), It.IsAny<string>()))
            .ReturnsAsync(updated);

        var result = await controller.UpdateStatus(
            3, new UpdateDisputeStatusRequest(DisputeStatus.UnderReview, "Reviewing"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<DisputeResponse>(ok.Value);
        Assert.Equal("UnderReview", body.Status);
    }

    [Fact]
    public async Task UpdateStatus_PassesAdminNameToService()
    {
        var (controller, mock) = Create(adminName: "Jane Admin");
        mock.Setup(s => s.UpdateDisputeStatusAsync(
            It.IsAny<int>(), It.IsAny<UpdateDisputeStatusRequest>(), It.IsAny<string>()))
            .ReturnsAsync(MakeDispute());

        await controller.UpdateStatus(1, new UpdateDisputeStatusRequest(DisputeStatus.UnderReview, null));

        mock.Verify(s => s.UpdateDisputeStatusAsync(
            1, It.IsAny<UpdateDisputeStatusRequest>(), "Jane Admin"), Times.Once);
    }
}
