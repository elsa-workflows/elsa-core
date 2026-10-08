using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Admission;

// The public management facade cannot mutate admission-owned state. Execution-internal
// variable persistence remains on the separately audited storage/persistence path.
internal sealed class AdmissionWorkflowInstanceVariableManager(IWorkflowInstanceVariableManager inner, IWorkflowExecutionGuard guard)
    : IWorkflowInstanceVariableManager
{
    public Task<IEnumerable<ResolvedVariable>> GetVariablesAsync(string instanceId, IEnumerable<string>? excludeTags = null, CancellationToken cancellationToken = default) =>
        inner.GetVariablesAsync(instanceId, excludeTags, cancellationToken);
    public Task<IEnumerable<ResolvedVariable>> GetVariablesAsync(WorkflowInstance instance, IEnumerable<string>? excludeTags = null, CancellationToken cancellationToken = default) =>
        inner.GetVariablesAsync(instance, excludeTags, cancellationToken);
    public Task<IEnumerable<ResolvedVariable>> GetVariablesAsync(WorkflowState state, IEnumerable<string>? excludeTags = null, CancellationToken cancellationToken = default) =>
        inner.GetVariablesAsync(state, excludeTags, cancellationToken);
    public Task<IEnumerable<ResolvedVariable>> GetVariablesAsync(WorkflowExecutionContext context, IEnumerable<string>? excludeTags = null, CancellationToken cancellationToken = default) =>
        inner.GetVariablesAsync(context, excludeTags, cancellationToken);

    public async Task<IEnumerable<ResolvedVariable>> SetVariablesAsync(string instanceId, IEnumerable<VariableUpdateValue> variables, CancellationToken cancellationToken = default)
    {
        await guard.DemandUnownedAsync(instanceId, cancellationToken);
        return await inner.SetVariablesAsync(instanceId, variables, cancellationToken);
    }
    public async Task<IEnumerable<ResolvedVariable>> SetVariablesAsync(WorkflowExecutionContext context, IEnumerable<VariableUpdateValue> variables, CancellationToken cancellationToken = default)
    {
        await guard.AuthorizeAsync(context, WorkflowExecutionEntryPoint.DirectPipeline);
        return await inner.SetVariablesAsync(context, variables, cancellationToken);
    }
}
