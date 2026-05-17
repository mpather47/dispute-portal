
namespace DisputePortal.Api.Models
{
    public class BankTransaction
    {
        public int Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string MerchantName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsDisputable { get; set; } = true;

        public int AccountId { get; set; }
        public Account Account { get; set; } = null!;

        public Dispute? Dispute { get; set; }
    }
}
