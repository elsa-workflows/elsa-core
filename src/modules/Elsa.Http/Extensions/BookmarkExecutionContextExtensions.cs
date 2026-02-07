using Elsa.Expressions.Models;
using Elsa.Http;
using Elsa.SasTokens.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Api;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for working with <see cref="ExpressionExecutionContext"/> and generating bookmark trigger URLs.
/// </summary>
public static class BookmarkExecutionContextExtensions
{
    extension(ActivityExecutionContext context)
    {
        public string GenerateBookmarkTriggerUrl(string bookmarkId, TimeSpan lifetime) => context.ExpressionExecutionContext.GenerateBookmarkTriggerUrl(bookmarkId, lifetime);
        public string GenerateBookmarkTriggerUrl(string bookmarkId, DateTimeOffset expiresAt) => context.ExpressionExecutionContext.GenerateBookmarkTriggerUrl(bookmarkId, expiresAt);
        public string GenerateBookmarkTriggerUrl(string bookmarkId) => context.ExpressionExecutionContext.GenerateBookmarkTriggerUrl(bookmarkId);
        public string GenerateBookmarkTriggerToken(string bookmarkId, TimeSpan? lifetime = null, DateTimeOffset? expiresAt = null) => context.ExpressionExecutionContext.GenerateBookmarkTriggerToken(bookmarkId, lifetime, expiresAt);
    }

    /// <param name="context">The expression execution context.</param>
    extension(ExpressionExecutionContext context)
    {
        /// <summary>
        /// Generates a URL that can be used to resume a bookmarked workflow.
        /// </summary>
        /// <param name="bookmarkId">The ID of the bookmark to resume.</param>
        /// <param name="lifetime">The lifetime of the bookmark trigger token.</param>
        /// <returns>A URL that can be used to resume a bookmarked workflow.</returns>
        public string GenerateBookmarkTriggerUrl(string bookmarkId, TimeSpan lifetime)
        {
            var token = context.GenerateBookmarkTriggerToken(bookmarkId, lifetime);
            return context.GenerateBookmarkTriggerUrlInternal(token);
        }

        /// <summary>
        /// Generates a URL that can be used to resume a bookmarked workflow.
        /// </summary>
        /// <param name="bookmarkId">The ID of the bookmark to resume.</param>
        /// <param name="expiresAt">The expiration date of the bookmark trigger token.</param>
        /// <returns>A URL that can be used to resume a bookmarked workflow.</returns>
        public string GenerateBookmarkTriggerUrl(string bookmarkId, DateTimeOffset expiresAt)
        {
            var token = context.GenerateBookmarkTriggerToken(bookmarkId, expiresAt: expiresAt);
            return context.GenerateBookmarkTriggerUrlInternal(token);
        }

        /// <summary>
        /// Generates a URL that can be used to resume a bookmarked workflow.
        /// </summary>
        /// <param name="bookmarkId">The ID of the bookmark to resume.</param>
        /// <returns>A URL that can be used to trigger an event.</returns>
        public string GenerateBookmarkTriggerUrl(string bookmarkId)
        {
            var token = context.GenerateBookmarkTriggerToken(bookmarkId);
            return context.GenerateBookmarkTriggerUrlInternal(token);
        }

        public string GenerateBookmarkTriggerToken(string bookmarkId, TimeSpan? lifetime = null, DateTimeOffset? expiresAt = null)
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
        
        private string GenerateBookmarkTriggerUrlInternal(string token)
        {
            var options = context.GetRequiredService<IOptions<ApiEndpointOptions>>().Value;
            var url = $"{options.RoutePrefix}/bookmarks/resume?t={token}";
            var absoluteUrlProvider = context.GetRequiredService<IAbsoluteUrlProvider>();
            return absoluteUrlProvider.ToAbsoluteUrl(url).ToString();
        }
    }
}