using System.Diagnostics;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>
/// Produces the deliberately small, non-sensitive error contract exposed by the authentication broker.
/// </summary>
public static class BrokerErrorFactory
{
    public static PublicBrokerError Create(BrokerErrorCategory category, string? correlationId = null)
    {
        var (error, message) = category switch
        {
            BrokerErrorCategory.InvalidRequest => ("invalid_request", "The sign-in request is invalid. Start again."),
            BrokerErrorCategory.MethodUnavailable => ("method_unavailable", "This sign-in method is unavailable."),
            BrokerErrorCategory.AuthenticationFailed => ("authentication_failed", "Authentication could not be completed."),
            BrokerErrorCategory.IdentityUnlinked => ("identity_unlinked", "This identity is not permitted to sign in."),
            BrokerErrorCategory.FlowExpired => ("flow_expired", "The sign-in attempt expired. Start again."),
            BrokerErrorCategory.FlowChanged => ("flow_changed", "The sign-in method changed. Start again."),
            BrokerErrorCategory.AccessDenied => ("access_denied", "Access was denied."),
            BrokerErrorCategory.RateLimited => ("rate_limited", "Too many sign-in attempts. Try again later."),
            BrokerErrorCategory.TemporarilyUnavailable => ("temporarily_unavailable", "Sign-in is temporarily unavailable. Try again later."),
            BrokerErrorCategory.ServerError => ("server_error", "Sign-in could not be completed. Try again later."),
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        };

        return new PublicBrokerError(error, message, IsSafeCorrelationId(correlationId) ? correlationId! : CreateCorrelationId());
    }

    public static string CreateCorrelationId()
    {
        var traceId = Activity.Current?.TraceId.ToString();
        return !string.IsNullOrWhiteSpace(traceId) && traceId != "00000000000000000000000000000000"
            ? traceId
            : ActivityTraceId.CreateRandom().ToString();
    }

    private static bool IsSafeCorrelationId(string? correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 128)
            return false;

        return correlationId.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
    }
}
