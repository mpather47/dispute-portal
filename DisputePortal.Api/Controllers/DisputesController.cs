using System.Security.Claims;
using DisputePortal.Api.DTOs;
using DisputePortal.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DisputePortal.Api.Controllers;

[ApiController]
[Route("api/disputes")]
[Authorize]
public class DisputesController : ControllerBase
{
    private readonly DisputeService _disputeService;

    public DisputesController(DisputeService disputeService)
    {
        _disputeService = disputeService;
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<DisputeResponse>> CreateDispute(
        CreateDisputeRequest request)
    {
        try
        {
            var userId = GetUserId();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "User ID claim is missing." });
            }

            var dispute = await _disputeService.CreateDisputeAsync(userId, request);

            return CreatedAtAction(
                nameof(GetDisputeById),
                new { id = dispute.Id },
                dispute
            );
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("my")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<List<DisputeResponse>>> GetMyDisputes()
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { message = "User ID claim is missing." });
        }

        var disputes = await _disputeService.GetCustomerDisputesAsync(userId);

        return Ok(disputes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DisputeResponse>> GetDisputeById(int id)
    {
        try
        {
            var userId = GetUserId();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "User ID claim is missing." });
            }

            var isAdmin = User.IsInRole("Admin");

            var dispute = await _disputeService.GetDisputeByIdAsync(
                id,
                userId,
                isAdmin
            );

            return Ok(dispute);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}