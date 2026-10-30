using System.Net;
using Elsa.Studio.Localization;
using Refit;

namespace Elsa.Studio.Extensions;

/// <summary>
/// Presents a backend's 401 or 403 response to the user as guidance, instead of the raw
/// "Response status code does not indicate success: 403 (Forbidden)." the API client reports.
/// </summary>
public static class AuthorizationFailureExtensions
{
    /// <summary>Shown when the backend refuses a request because the user lacks a permission.</summary>
    public const string ForbiddenMessage = "You don't have permission to do this. Ask an administrator for access.";

    /// <summary>Shown when the backend no longer accepts the user's sign-in.</summary>
    public const string UnauthorizedMessage = "Your session has expired. Sign in again to continue.";

    /// <summary>Returns the guidance for a 401 or 403 status, or <c>null</c> for any other status.</summary>
    public static string? GetAuthorizationFailureMessage(this HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Forbidden => ForbiddenMessage,
        HttpStatusCode.Unauthorized => UnauthorizedMessage,
        _ => null
    };

    /// <summary>
    /// Describes a response that came back without a body of its own: guidance for a 401 or 403, otherwise the reason
    /// phrase, otherwise <paramref name="fallback"/>.
    /// </summary>
    public static string GetEmptyBodyFailureText(this HttpStatusCode statusCode, string? reasonPhrase, string fallback) =>
        statusCode.GetAuthorizationFailureMessage() ?? reasonPhrase ?? fallback;

    /// <summary>Whether the exception reports a 401 or 403 response from the backend.</summary>
    public static bool IsAuthorizationFailure(this Exception exception) => FindAuthorizationFailureMessage(exception) != null;

    /// <summary>Whether the exception reports a 401 response: the backend no longer accepts the user's sign-in.</summary>
    public static bool IsUnauthorizedResponse(this Exception exception) => FindResponseStatus(exception) == HttpStatusCode.Unauthorized;

    /// <summary>
    /// Returns the guidance for an exception that reports a 401 or 403 response, or <c>null</c> for any other exception,
    /// so a caller with its own fixed text can write <c>e.GetAuthorizationFailureMessage() ?? "fixed text"</c>.
    /// </summary>
    public static string? GetAuthorizationFailureMessage(this Exception exception, ILocalizer? localizer = null) =>
        FindAuthorizationFailureMessage(exception) is { } message ? localizer?[message].Value ?? message : null;

    /// <summary>
    /// Returns the text to show the user for the exception: guidance for a 401 or 403 response, the exception's own
    /// message for anything else.
    /// </summary>
    public static string ToUserMessage(this Exception exception, ILocalizer? localizer = null) =>
        exception.GetAuthorizationFailureMessage(localizer) ?? exception.Message;

    private static string? FindAuthorizationFailureMessage(Exception exception) => FindResponseStatus(exception)?.GetAuthorizationFailureMessage();

    // The first exception in the chain that carries a response status decides: a 403 wrapped by another exception is
    // still recognized, and a response with any other status is never reinterpreted because of what it wraps.
    private static HttpStatusCode? FindResponseStatus(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            var statusCode = current switch
            {
                ApiException apiException => apiException.StatusCode,
                HttpRequestException { StatusCode: { } status } => status,
                _ => (HttpStatusCode?)null
            };

            if (statusCode != null)
                return statusCode;
        }

        return null;
    }
}
