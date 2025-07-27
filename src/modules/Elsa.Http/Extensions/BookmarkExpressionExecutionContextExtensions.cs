using Elsa.Expressions.Models;
using Elsa.Http;
using Elsa.Http.Options;
using Elsa.SasTokens.Contracts;
using Elsa.Workflows.Api;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for working with <see cref="ExpressionExecutionContext"/> and generating bookmark trigger URLs.
/// </summary>
public static class BookmarkExpressionExecutionContextExtensions
{
    /// <summary>
    /// Generates a URL that can be used to resume a bookmarked workflow.
    /// </summary>
    /// <param name="context">The expression execution context.</param>
    /// <param name="bookmarkId">The ID of the bookmark to resume.</param>
    /// <param name="lifetime">The lifetime of the bookmark trigger token.</param>
    /// <returns>A URL that can be used to resume a bookmarked workflow.</returns>
    public static string GenerateBookmarkTriggerUrl(this ExpressionExecutionContext context, string bookmarkId, TimeSpan lifetime)
    {
        var token = context.GenerateBookmarkTriggerTokenInternal(bookmarkId, lifetime);
        return context.GenerateBookmarkTriggerUrlInternal(token);
    }

    /// <summary>
    /// Generates a URL that can be used to trigger an event.
    /// </summary>
    /// <param name="context">The expression execution context.</param>
    /// <param name="bookmarkId">The ID of the bookmark to resume.</param>
    /// <param name="expiresAt">The expiration date of the event trigger token.</param>
    /// <returns>A URL that can be used to trigger an event.</returns>
    public static string GenerateBookmarkTriggerUrl(this ExpressionExecutionContext context, string bookmarkId, DateTimeOffset expiresAt)
    {
        var token = context.GenerateBookmarkTriggerTokenInternal(bookmarkId, expiresAt: expiresAt);
        return context.GenerateBookmarkTriggerUrlInternal(token);
    }

    /// <summary>
    /// Generates a URL that can be used to trigger an event.
    /// </summary>
    /// <param name="context">The expression execution context.</param>
    /// <param name="bookmarkId">The ID of the bookmark to resume.</param>
    /// <returns>A URL that can be used to trigger an event.</returns>
    public static string GenerateBookmarkTriggerUrl(this ExpressionExecutionContext context, string bookmarkId)
    {
        var token = context.GenerateBookmarkTriggerTokenInternal(bookmarkId);
        return context.GenerateBookmarkTriggerUrlInternal(token);
    }

    private static string GenerateBookmarkTriggerUrlInternal(this ExpressionExecutionContext context, string token)
    {
        var options = context.GetRequiredService<IOptions<ApiEndpointOptions>>().Value;
        var url = $"{options.RoutePrefix}/bookmarks/resume?t={token}";
        var absoluteUrlProvider = context.GetRequiredService<IAbsoluteUrlProvider>();
        return absoluteUrlProvider.ToAbsoluteUrl(url).ToString();
    }

    private static string GenerateBookmarkTriggerTokenInternal(this ExpressionExecutionContext context, string bookmarkId, TimeSpan? lifetime = null, DateTimeOffset? expiresAt = null)
    {
        var workflowInstanceId = context.GetWorkflowExecutionContext().Id;
        var payload = new BookmarkTokenPayload(bookmarkId, workflowInstanceId);
        var tokenService = context.GetRequiredService<ITokenService>();

        return lifetime != null
            ? tokenService.CreateToken(payload, lifetime.Value)
            : expiresAt != null
                ? tokenService.CreateToken(payload, expiresAt.Value)
                : tokenService.CreateToken(payload);
    }
}