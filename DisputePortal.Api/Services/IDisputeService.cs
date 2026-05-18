using DisputePortal.Api.DTOs;
using DisputePortal.Api.Models;

namespace DisputePortal.Api.Services;

public interface IDisputeService
{
    Task<DisputeResponse> CreateDisputeAsync(string customerId, CreateDisputeRequest request);
    Task<List<TransactionResponse>> GetCustomerTransactionsAsync(string customerId);
    Task<List<DisputeResponse>> GetCustomerDisputesAsync(string customerId);
    Task<PagedResult<DisputeResponse>> GetAllDisputesForAdminAsync(int page, int pageSize, DisputeStatus? status = null, string? search = null);
    Task<DisputeResponse> GetDisputeByIdAsync(int disputeId, string userId, bool isAdmin);
    Task<DisputeResponse> UpdateDisputeStatusAsync(int disputeId, UpdateDisputeStatusRequest request, string adminName);
}
