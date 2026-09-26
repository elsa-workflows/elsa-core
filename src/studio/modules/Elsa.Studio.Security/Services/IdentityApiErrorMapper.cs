using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Elsa.Studio.Security.Models;
using Refit;

namespace Elsa.Studio.Security.Services;

/// <summary>
/// Maps structured Core failures without exposing raw response bodies or exception details to the UI.
/// </summary>
public static class IdentityApiErrorMapper
{
    public const string RoleSubject = "role";
    public const string UserSubject = "user";

    /// <param name="exception">The failure raised by the Identity API client.</param>
    /// <param name="subject">The administered subject used in fallback messages, such as <see cref="RoleSubject"/> or <see cref="UserSubject"/>.</param>
    public static IdentityApiErrorInfo Describe(Exception exception, string subject = RoleSubject) => exception switch
    {
        ApiException apiException => FromApi(apiException, subject),
        OperationCanceledException => new("cancelled", "The request was cancelled."),
        HttpRequestException => new("offline", "Core could not be reached. Check the connection and try again."),
        _ => IdentityApiErrorInfo.UnavailableFor(subject)
    };

    private static IdentityApiErrorInfo FromApi(ApiException exception, string subject)
    {
        var structured = TryReadStructuredError(exception.Content);
        var validation = TryReadValidationError(exception.Content);
        var isAuthorization = exception.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
        var isNotFound = exception.StatusCode == HttpStatusCode.NotFound;
        var isConflict = exception.StatusCode == HttpStatusCode.Conflict;
        var isValidation = exception.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity;

        if (structured is { Error.Length: > 0, Message.Length: > 0 })
            return new(structured.Error, structured.Message, isAuthorization, isNotFound, isConflict, isValidation);

        if (validation is { Message.Length: > 0 })
        {
            var details = validation.Errors
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .SelectMany(x => x.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x));
            var message = string.Join(" ", details);
            return new("validation_failed", string.IsNullOrWhiteSpace(message) ? validation.Message : message,
                isAuthorization, isNotFound, isConflict, IsValidation: true);
        }

        return exception.StatusCode switch
        {
            HttpStatusCode.BadRequest => new("invalid", "The request did not pass validation.", IsValidation: true),
            HttpStatusCode.Unauthorized => new("unauthorized", "Your session has expired. Sign in again to continue.", IsAuthorization: true),
            HttpStatusCode.Forbidden => new("forbidden", $"You are not allowed to perform this {subject} administration action.", IsAuthorization: true),
            HttpStatusCode.NotFound => new("not_found", $"The {subject} no longer exists.", IsNotFound: true),
            HttpStatusCode.Conflict => new("conflict", $"The {subject} or one of its dependencies changed. Refresh and review the latest state.", IsConflict: true),
            HttpStatusCode.TooManyRequests => new("throttled", "Too many requests. Wait a moment and try again."),
            _ => IdentityApiErrorInfo.UnavailableFor(subject)
        };
    }

    private static CoreApiErrorResponse? TryReadStructuredError(string? content) =>
        TryDeserialize(content, IdentityJsonSerializerContext.Default.CoreApiErrorResponse);

    private static ValidationApiErrorResponse? TryReadValidationError(string? content) =>
        TryDeserialize(content, IdentityJsonSerializerContext.Default.ValidationApiErrorResponse);

    private static T? TryDeserialize<T>(string? content, JsonTypeInfo<T> typeInfo)
    {
        if (string.IsNullOrWhiteSpace(content))
            return default;

        try
        {
            return JsonSerializer.Deserialize(content, typeInfo);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
