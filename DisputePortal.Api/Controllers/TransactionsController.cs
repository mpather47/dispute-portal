using DisputePortal.Api.DTOs;
using DisputePortal.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DisputePortal.Api.Controllers;

[Route("api/transactions")]
[Authorize(Roles = "Customer")]
public class TransactionsController : ApiControllerBase
{
    private readonly IDisputeService _disputeService;

    public TransactionsController(IDisputeService disputeService)
    {
        _disputeService = disputeService;
    }

    [HttpGet]
    public async Task<ActionResult<List<TransactionResponse>>> GetTransactions()
    {
        var transactions = await _disputeService.GetCustomerTransactionsAsync(GetUserId());
        return Ok(transactions);
    }
}
