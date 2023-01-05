using Elsa.Mediator.Models;
using Elsa.Mediator.Services;
using Elsa.Webhooks.Commands;
using Elsa.Webhooks.Services;

namespace Elsa.Webhooks.Handlers;

/// <summary>
/// Handles the <see cref="InvokeWebhook"/> command. 
/// </summary>
public class InvokeWebhookHandler : ICommandHandler<InvokeWebhook>
{
    private readonly IWebhookInvoker _webhookInvoker;

    /// <summary>
    /// Constructor.
    /// </summary>
    public InvokeWebhookHandler(IWebhookInvoker webhookInvoker)
    {
        _webhookInvoker = webhookInvoker;
    }
    
    /// <inheritdoc />
    public async Task<Unit> HandleAsync(InvokeWebhook command, CancellationToken cancellationToken)
    {
        await _webhookInvoker.InvokeWebhookAsync(command.WebhookRegistration, command.WebhookEvent, cancellationToken);
        return Unit.Instance;
    }
}