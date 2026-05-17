using Microsoft.AspNetCore.Identity;

namespace DisputePortal.Api.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public List<Account> Accounts { get; set; } = new();
}