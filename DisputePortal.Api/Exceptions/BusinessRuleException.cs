namespace DisputePortal.Api.Exceptions;

/// <summary>
/// Thrown for user-visible business-rule violations whose message is safe to expose to the client.
/// Extends InvalidOperationException so existing catch-sites and tests still compile without changes.
/// </summary>
public sealed class BusinessRuleException : InvalidOperationException
{
    public BusinessRuleException(string message) : base(message) { }
}
