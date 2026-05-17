using System.Security.Claims;
using DisputePortal.Api.DTOs;
using DisputePortal.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DisputePortal.Api.Controllers;

[ApiController]
[Route("api/admin/disputes")]
[Authorize(Roles = "Admin")]
public class AdminDisputesController : ControllerBase
{
    private readonly DisputeService _disputeService;

    public AdminDisputesController(DisputeService disputeService)
    {
        _disputeService = disputeService;
    }

    [HttpGet]
    public async Task<ActionResult<List<DisputeResponse>>> GetAllDisputes()
    {
        var disputes = await _disputeService.GetAllDisputesForAdminAsync();

        return Ok(disputes);
    }

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<DisputeResponse>> UpdateStatus(
        int id,
        UpdateDisputeStatusRequest request)
    {
        try
        {
            var adminName = User.FindFirstValue(ClaimTypes.Name) ?? "Admin";

            var dispute = await _disputeService.UpdateDisputeStatusAsync(
                id,
                request,
                adminName
            );

            return Ok(dispute);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}