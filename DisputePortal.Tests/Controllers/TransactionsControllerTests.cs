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

public class TransactionsControllerTests
{
    private static (TransactionsController controller, Mock<IDisputeService> mock) Create(
        string userId = "user-1")
    {
        var mock = new Mock<IDisputeService>();
        var controller = new TransactionsController(mock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Role, "Customer"),
                ], "Test"))
            }
        };
        return (controller, mock);
    }

    private static TransactionResponse MakeTransaction(int id) => new(
        id, $"TXN-00{id}", "Test Merchant", 99.99m,
        TransactionType.Debit.ToString(), DateTime.UtcNow,
        "Purchase", true, false);

    [Fact]
    public async Task GetTransactions_Returns200WithList()
    {
        var (controller, mock) = Create();
        var transactions = new List<TransactionResponse>
        {
            MakeTransaction(1),
            MakeTransaction(2),
        };
        mock.Setup(s => s.GetCustomerTransactionsAsync("user-1")).ReturnsAsync(transactions);

        var result = await controller.GetTransactions();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<List<TransactionResponse>>(ok.Value);
        Assert.Equal(2, body.Count);
    }

    [Fact]
    public async Task GetTransactions_PassesUserIdToService()
    {
        var (controller, mock) = Create(userId: "user-99");
        mock.Setup(s => s.GetCustomerTransactionsAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        await controller.GetTransactions();

        mock.Verify(s => s.GetCustomerTransactionsAsync("user-99"), Times.Once);
    }

    [Fact]
    public async Task GetTransactions_EmptyList_Returns200WithEmptyList()
    {
        var (controller, mock) = Create();
        mock.Setup(s => s.GetCustomerTransactionsAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        var result = await controller.GetTransactions();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<List<TransactionResponse>>(ok.Value);
        Assert.Empty(body);
    }
}
