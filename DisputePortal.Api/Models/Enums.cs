namespace DisputePortal.Api.Models
{
    public enum UserRole
    {
        Customer = 1,
        Admin = 2
    }

    public enum TransactionType
    {
        Debit = 1,
        Credit = 2
    }

    public enum DisputeStatus
    {
        Submitted = 1,
        UnderReview = 2,
        MoreInfoRequired = 3,
        Approved = 4,
        Rejected = 5,
        Resolved = 6
    }
}
