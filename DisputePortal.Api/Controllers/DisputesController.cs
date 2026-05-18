using DisputePortal.Api.DTOs;
using DisputePortal.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;

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

    [HttpPost("{id:int}/reply")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<DisputeResponse>> ReplyToDispute(int id, ReplyRequest request)
    {
        var dispute = await _disputeService.ReplyToDisputeAsync(
            id,
            GetUserId(),
            GetUserName(),
            request
        );

        return Ok(dispute);
    }

    [HttpPost("{id:int}/attachments")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [EnableRateLimiting("upload")]
    public async Task<ActionResult<AttachmentResponse>> UploadAttachment(int id, IFormFile file)
    {
        var attachment = await _disputeService.AddAttachmentAsync(
            id,
            GetUserId(),
            GetUserName(),
            file
        );

        return Ok(attachment);
    }

    [HttpGet("{id:int}/attachments/{attachmentId:int}")]
    public async Task<IActionResult> DownloadAttachment(int id, int attachmentId)
    {
        var (data, contentType, fileName) = await _disputeService.GetAttachmentAsync(
            id,
            GetUserId(),
            User.IsInRole("Admin"),
            attachmentId
        );

        return File(data, contentType, fileName);
    }
}
