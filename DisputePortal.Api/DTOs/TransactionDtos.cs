namespace DisputePortal.Api.DTOs
{
    public record TransactionResponse(
    int Id,
    string Reference,
    string MerchantName,
    decimal Amount,
    string Type,
    DateTime TransactionDate,
    string Description,
    bool IsDisputable,
    bool HasDispute
);
}
