using DisputePortal.Api.DTOs;
using DisputePortal.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DisputePortal.Api.Controllers;

[Route("api/admin/disputes")]
[Authorize(Roles = "Admin")]
public class AdminDisputesController : ApiControllerBase
{
    private readonly IDisputeService _disputeService;

    public AdminDisputesController(IDisputeService disputeService)
    {
        _disputeService = disputeService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<DisputeResponse>>> GetAllDisputes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await _disputeService.GetAllDisputesForAdminAsync(page, pageSize);
        return Ok(result);
    }

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<DisputeResponse>> UpdateStatus(
        int id,
        UpdateDisputeStatusRequest request)
    {
        var dispute = await _disputeService.UpdateDisputeStatusAsync(
            id,
            request,
            GetUserName()
        );

        return Ok(dispute);
    }
}
