using Elsa.Common.Multitenancy;
using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Tenants.Mediator;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Dispatches workflow cancellation requests to a local background worker.
/// </summary>
public class BackgroundWorkflowCancellationDispatcher(ICommandSender commandSender, ITenantAccessor tenantAccessor) : IWorkflowCancellationDispatcher
{
    /// <inheritdoc />
    public async Task<DispatchCancelWorkflowsResponse> DispatchAsync(DispatchCancelWorkflowRequest request, CancellationToken cancellationToken = default)
    {
        var command = new CancelWorkflowsCommand(request);
        await commandSender.SendAsync(command, CommandStrategy.Background, cancellationToken);
        return new DispatchCancelWorkflowsResponse();
    }
    
    
    private IDictionary<object, object> CreateHeaders()
    {
        return TenantHeaders.CreateHeaders(tenantAccessor.Tenant?.Id);
    }
}