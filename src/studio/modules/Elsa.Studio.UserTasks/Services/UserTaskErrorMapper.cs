using System.Net;
using System.Text.Json;
using Elsa.Studio.UserTasks.Models;
using Refit;

namespace Elsa.Studio.UserTasks.Services;

/// <summary>
/// A safe, display-ready description of a failed request. Raw exception text never reaches the UI: it can
/// carry URLs, headers, and payload fragments that the disclosure rules keep off the screen.
/// </summary>
public sealed record UserTaskErrorInfo(string Code, string Message, bool RequiresReload, bool IsAuthorization)
{
    public static readonly UserTaskErrorInfo Unavailable = new("unavailable", "Tasks are unavailable right now. Try again in a moment.", false, false);
}

public static class UserTaskErrorMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static UserTaskErrorInfo Describe(Exception exception) => exception switch
    {
        ApiException api => FromApi(api),
        OperationCanceledException => new("cancelled", "The request was cancelled.", false, false),
        HttpRequestException => new("offline", "The server could not be reached. Check your connection and try again.", false, false),
        _ => UserTaskErrorInfo.Unavailable
    };

    private static UserTaskErrorInfo FromApi(ApiException exception)
    {
        var body = TryReadBody(exception);
        var requiresReload = exception.StatusCode == HttpStatusCode.Conflict;
        var isAuthorization = exception.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized;

        if (body is { } error && !string.IsNullOrWhiteSpace(error.Message))
            return new(error.Code, error.Message, requiresReload, isAuthorization);

        return exception.StatusCode switch
        {
            HttpStatusCode.NotFound => new("not-found", "This task is no longer available.", false, false),
            HttpStatusCode.Forbidden => new("forbidden", "You are not allowed to perform this action on this task.", false, true),
            HttpStatusCode.Unauthorized => new("unauthorized", "Your session has expired. Sign in again to continue.", false, true),
            HttpStatusCode.Conflict => new("conflict", "The task changed since it was loaded. Reload it and try again.", true, false),
            HttpStatusCode.UnprocessableEntity => new("invalid", "The response did not pass validation.", false, false),
            HttpStatusCode.TooManyRequests => new("throttled", "Too many attempts. Wait a moment and try again.", false, false),
            _ => UserTaskErrorInfo.Unavailable
        };
    }

    private static UserTaskErrorResponse? TryReadBody(ApiException exception)
    {
        if (string.IsNullOrWhiteSpace(exception.Content))
            return null;
        try
        {
            return JsonSerializer.Deserialize<UserTaskErrorResponse>(exception.Content, JsonOptions);
        }
        catch (JsonException)
        {
            // A non-conforming body (a proxy error page, for example) is discarded rather than displayed.
            return null;
        }
    }
}
