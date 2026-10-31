namespace Elsa.Workflows.Admission;

internal sealed class AdmissionExecutionGuard(IAdmissionStore store, AdmissionAuthorityRegistry authorities, IServiceProvider services) : IWorkflowExecutionGuard
{
    public async ValueTask DemandUnownedAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        if (await store.FindByInstanceAsync(instanceId, cancellationToken) != null)
        {
            throw new InvalidOperationException("This workflow instance is owned by durable admission.");
        }
    }

    public async ValueTask<IWorkflowExecutionAuthorization?> AuthorizeAsync(WorkflowExecutionContext context, WorkflowExecutionEntryPoint entryPoint)
    {
        // Consult the private exact-context binding before mutable context.Id: changing an owned
        // context to an unowned ID cannot escape authorization/fingerprint validation.
        var authorization = authorities.Consume(context, entryPoint);
        if (authorization != null)
        {
            if (services.GetService(typeof(IAdmissionExecutionObserver)) is IAdmissionExecutionObserver observer)
            {
                await observer.ObserveAsync(AdmissionExecutionBoundary.AuthorityConsumed, authorization.AdmissionId, authorization.InstanceId, context.CancellationToken);
            }
            return authorization;
        }
        await DemandUnownedAsync(context.Id, context.CancellationToken);
        return null;
    }
}
