using DisputePortal.Api.DTOs;
using DisputePortal.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DisputePortal.Api.Controllers;

[Route("api/disputes")]
[Authorize]
public class DisputesController : ApiControllerBase
{
    private readonly IDisputeService _disputeService;

    public DisputesController(IDisputeService disputeService)
    {
        _disputeService = disputeService;
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<DisputeResponse>> CreateDispute(
        CreateDisputeRequest request)
    {
        var dispute = await _disputeService.CreateDisputeAsync(GetUserId(), request);

        return CreatedAtAction(
            nameof(GetDisputeById),
            new { id = dispute.Id },
            dispute
        );
    }

    [HttpGet("my")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<List<DisputeResponse>>> GetMyDisputes()
    {
        var disputes = await _disputeService.GetCustomerDisputesAsync(GetUserId());
        return Ok(disputes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DisputeResponse>> GetDisputeById(int id)
    {
        var dispute = await _disputeService.GetDisputeByIdAsync(
            id,
            GetUserId(),
            User.IsInRole("Admin")
        );

        return Ok(dispute);
    }
}
