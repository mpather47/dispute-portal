using System.Security.Claims;
using DisputePortal.Api.DTOs;
using DisputePortal.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DisputePortal.Api.Controllers;

[ApiController]
[Route("api/transactions")]
[Authorize(Roles = "Customer")]
public class TransactionsController : ControllerBase
{
    private readonly DisputeService _disputeService;

    public TransactionsController(DisputeService disputeService)
    {
        _disputeService = disputeService;
    }

    [HttpGet]
    public async Task<ActionResult<List<TransactionResponse>>> GetTransactions()
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { message = "User ID claim is missing." });
        }

        var transactions = await _disputeService.GetCustomerTransactionsAsync(userId);

        return Ok(transactions);
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}