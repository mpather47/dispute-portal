using DisputePortal.Api.DTOs;

namespace DisputePortal.Api.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
}
