namespace Elsa.Webhooks.Models;

/// <summary>
/// Stores payload information about the RunTask webhook event type.
/// </summary>
public record RunTaskWebhook(string TaskId, string TaskName, object? TaskParams);