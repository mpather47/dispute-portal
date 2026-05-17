namespace DisputePortal.Api.DTOs
{
    public record LoginRequest(string Email, string Password);

    public record LoginResponse(
    string Token,
    string UserId,
    string FullName,
    string Email,
    string Role
);
}
