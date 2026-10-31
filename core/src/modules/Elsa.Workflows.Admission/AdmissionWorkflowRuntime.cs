using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Admission;

/// <summary>Actual isolated LocalWorkflowRuntime client factory; initial admission authority is never exposed on clients.</summary>
public sealed partial class AdmissionWorkflowRuntime : IWorkflowRuntime
{
    private readonly LocalWorkflowRuntime _local;
    private readonly IAdmissionStore _store;
    private readonly AdmissionExecutionService _execution;
    private readonly IWorkflowExecutionGuard _guard;
    private readonly Lazy<ObsoleteWorkflowRuntime> _obsoleteApi;
    private ObsoleteWorkflowRuntime ObsoleteApi => _obsoleteApi.Value;

    public AdmissionWorkflowRuntime(IServiceProvider services, IIdentityGenerator identityGenerator, IAdmissionStore store,
        AdmissionExecutionService execution, IWorkflowExecutionGuard guard)
    {
        _local = new LocalWorkflowRuntime(services, identityGenerator);
        _store = store;
        _execution = execution;
        _guard = guard;
        // The legacy facade closes over THIS factory, not LocalWorkflowRuntime's private one.
        _obsoleteApi = new(() => ObsoleteWorkflowRuntime.Create(services, CreateClientAsync));
    }

    public ValueTask<IWorkflowClient> CreateClientAsync(CancellationToken cancellationToken = default) => CreateClientAsync(null, cancellationToken);
    public async ValueTask<IWorkflowClient> CreateClientAsync(string? workflowInstanceId, CancellationToken cancellationToken = default)
    {
        var inner = await _local.CreateClientAsync(workflowInstanceId, cancellationToken);
        return new Client(inner, _store, _execution, _guard);
    }

    private sealed class Client(IWorkflowClient inner, IAdmissionStore store, AdmissionExecutionService execution, IWorkflowExecutionGuard guard) : IWorkflowClient
    {
        public string WorkflowInstanceId => inner.WorkflowInstanceId;
        public async Task<CreateWorkflowInstanceResponse> CreateInstanceAsync(CreateWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
        {
            await guard.DemandUnownedAsync(WorkflowInstanceId, cancellationToken);
            return await inner.CreateInstanceAsync(request, cancellationToken);
        }
        public async Task<RunWorkflowInstanceResponse> CreateAndRunInstanceAsync(CreateAndRunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
        {
            await guard.DemandUnownedAsync(WorkflowInstanceId, cancellationToken);
            return await inner.CreateAndRunInstanceAsync(request, cancellationToken);
        }
        public async Task<RunWorkflowInstanceResponse> RunInstanceAsync(RunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
        {
            var owned = await store.FindByInstanceAsync(WorkflowInstanceId, cancellationToken);
            return owned == null ? await inner.RunInstanceAsync(request, cancellationToken) : await execution.ResumeAsync(owned, request, cancellationToken);
        }
        public async Task<RunWorkflowInstanceResponse?> RunInstanceIfPendingAsync(RunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
        {
            var owned = await store.FindByInstanceAsync(WorkflowInstanceId, cancellationToken);
            if (owned != null)
            {
                // The admission execution service serialises resumption of an admitted instance;
                // keep the same run path, with the pending decision made against the stored state.
                var state = await inner.ExportStateAsync(cancellationToken);
                return state.SubStatus == WorkflowSubStatus.Pending
                    ? await execution.ResumeAsync(owned, request, cancellationToken)
                    : null;
            }

            return await inner.RunInstanceIfPendingAsync(request, cancellationToken);
        }
        public async Task CancelAsync(CancellationToken cancellationToken = default)
        {
            await guard.DemandUnownedAsync(WorkflowInstanceId, cancellationToken);
            await inner.CancelAsync(cancellationToken);
        }
        public Task<WorkflowState> ExportStateAsync(CancellationToken cancellationToken = default) => inner.ExportStateAsync(cancellationToken);
        public async Task ImportStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
        {
            await guard.DemandUnownedAsync(WorkflowInstanceId, cancellationToken);
            await guard.DemandUnownedAsync(workflowState.Id, cancellationToken);
            await inner.ImportStateAsync(workflowState, cancellationToken);
        }
        public Task<bool> InstanceExistsAsync(CancellationToken cancellationToken = default) => inner.InstanceExistsAsync(cancellationToken);
        public async Task<bool> DeleteAsync(CancellationToken cancellationToken = default)
        {
            await guard.DemandUnownedAsync(WorkflowInstanceId, cancellationToken);
            return await inner.DeleteAsync(cancellationToken);
        }
    }
}
