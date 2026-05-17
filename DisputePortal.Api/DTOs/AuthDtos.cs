using System.ComponentModel.DataAnnotations;

namespace DisputePortal.Api.DTOs;

public record LoginRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(128)] string Password
);

public record LoginResponse(
    string Token,
    string UserId,
    string FullName,
    string Email,
    string Role
);
