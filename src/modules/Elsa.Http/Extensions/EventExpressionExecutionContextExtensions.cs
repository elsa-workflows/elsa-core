using Elsa.Expressions.Models;
using Elsa.Http;
using Elsa.SasTokens.Contracts;
using Elsa.Workflows.Api;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for working with <see cref="ExpressionExecutionContext"/>.
/// </summary>
public static class EventExpressionExecutionContextExtensions
{
    /// <summary>
    /// Generates a URL that can be used to trigger an event.
    /// </summary>
    /// <param name="context">The expression execution context.</param>
    /// <param name="eventName">The name of the event to trigger.</param>
    /// <param name="lifetime">The lifetime of the event trigger token.</param>
    /// <returns>A URL that can be used to trigger an event.</returns>
    public static string GenerateEventTriggerUrl(this ExpressionExecutionContext context, string eventName, TimeSpan lifetime)
    {
        var token = context.GenerateEventTriggerTokenInternal(eventName, lifetime);
        return context.GenerateEventTriggerUrlInternal(token);
    }

    /// <summary>
    /// Generates a URL that can be used to trigger an event.
    /// </summary>
    /// <param name="context">The expression execution context.</param>
    /// <param name="eventName">The name of the event to trigger.</param>
    /// <param name="expiresAt">The expiration date of the event trigger token.</param>
    /// <returns>A URL that can be used to trigger an event.</returns>
    public static string GenerateEventTriggerUrl(this ExpressionExecutionContext context, string eventName, DateTimeOffset expiresAt)
    {
        var token = context.GenerateEventTriggerTokenInternal(eventName, expiresAt: expiresAt);
        return context.GenerateEventTriggerUrlInternal(token);
    }

    /// <summary>
    /// Generates a URL that can be used to trigger an event.
    /// </summary>
    /// <param name="context">The expression execution context.</param>
    /// <param name="eventName">The name of the event to trigger.</param>
    /// <returns>A URL that can be used to trigger an event.</returns>
    public static string GenerateEventTriggerUrl(this ExpressionExecutionContext context, string eventName)
    {
        var token = context.GenerateEventTriggerTokenInternal(eventName);
        return context.GenerateEventTriggerUrlInternal(token);
    }

    private static string GenerateEventTriggerUrlInternal(this ExpressionExecutionContext context, string token)
    {
        var options = context.GetRequiredService<IOptions<ApiEndpointOptions>>().Value;
        var url = $"{options.RoutePrefix}/events/trigger?t={token}";
        var absoluteUrlProvider = context.GetRequiredService<IAbsoluteUrlProvider>();
        return absoluteUrlProvider.ToAbsoluteUrl(url).ToString();
    }

    private static string GenerateEventTriggerTokenInternal(this ExpressionExecutionContext context, string eventName, TimeSpan? lifetime = null, DateTimeOffset? expiresAt = null)
    {
        var workflowInstanceId = context.GetWorkflowExecutionContext().Id;
        var payload = new EventTokenPayload(eventName, workflowInstanceId);
        var tokenService = context.GetRequiredService<ITokenService>();

        return lifetime != null
            ? tokenService.CreateToken(payload, lifetime.Value)
            : expiresAt != null
                ? tokenService.CreateToken(payload, expiresAt.Value)
                : tokenService.CreateToken(payload);
    }
}