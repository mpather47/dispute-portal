using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace DisputePortal.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID claim is missing.");
    }

    protected string GetUserName()
    {
        return User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "Unknown";
    }
}
