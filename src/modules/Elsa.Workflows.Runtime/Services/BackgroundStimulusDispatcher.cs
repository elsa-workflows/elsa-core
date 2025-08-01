using Elsa.Common.Multitenancy;
using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Tenants.Mediator;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// A simple implementation that queues the specified request for delivering stimuli on a non-durable background worker.
/// </summary>
public class BackgroundStimulusDispatcher(ICommandSender commandSender, ITenantAccessor tenantAccessor) : IStimulusDispatcher
{
    /// <inheritdoc />
    public async Task<DispatchStimulusResponse> SendAsync(DispatchStimulusRequest request, CancellationToken cancellationToken = default)
    {
        var command = new DispatchStimulusCommand(request);
        await commandSender.SendAsync(command, CommandStrategy.Background, CreateHeaders(), cancellationToken);
        return DispatchStimulusResponse.Empty;
    }

    private IDictionary<object, object> CreateHeaders()
    {
        return TenantHeaders.CreateHeaders(tenantAccessor.Tenant?.Id);
    }
}
