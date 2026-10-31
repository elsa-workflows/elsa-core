namespace Elsa.ExternalAuthentication.Models;

/// <summary>
/// The safe error categories that the external authentication broker may expose to a browser.
/// </summary>
public enum BrokerErrorCategory
{
    InvalidRequest,
    MethodUnavailable,
    AuthenticationFailed,
    IdentityUnlinked,
    FlowExpired,
    FlowChanged,
    AccessDenied,
    RateLimited,
    TemporarilyUnavailable,
    ServerError
}

/// <summary>
/// A browser-safe broker error. Diagnostic details must be retained only in internal telemetry.
/// </summary>
public sealed record PublicBrokerError(string Error, string Message, string CorrelationId);
