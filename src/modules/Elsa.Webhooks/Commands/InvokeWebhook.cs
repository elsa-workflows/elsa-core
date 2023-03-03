using Elsa.Mediator.Contracts;
using Elsa.Webhooks.Models;

namespace Elsa.Webhooks.Commands;

/// <summary>
/// Represents a command to invoke all registered webhook endpoints.
/// </summary>
public record InvokeWebhook(WebhookRegistration WebhookRegistration, WebhookEvent WebhookEvent) : ICommand;