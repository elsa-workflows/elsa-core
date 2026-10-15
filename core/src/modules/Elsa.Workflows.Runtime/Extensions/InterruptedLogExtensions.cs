using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Helpers that record <c>WorkflowInterrupted</c> entries in the per-instance workflow execution log.
/// </summary>
public static class InterruptedLogExtensions
{
    /// <summary>
    /// Writes a <c>WorkflowInterrupted</c> entry for the supplied instance with the typed payload. The record's
    /// <c>Id</c> is generated via <paramref name="identityGenerator"/> so the format stays consistent with every
    /// other log record in the store. Activity-related fields are populated from <paramref name="payload"/>'s
    /// <see cref="WorkflowInterruptedPayload.LastActivityId"/> when available; otherwise they are left empty
    /// (the forensic record still uniquely identifies the interrupted instance via the workflow instance id).
    /// </summary>
    public static async Task LogWorkflowInterruptedAsync(
        this IWorkflowExecutionLogStore store,
        IIdentityGenerator identityGenerator,
        WorkflowInstance instance,
        WorkflowInterruptedPayload payload,
        CancellationToken cancellationToken = default)
    {
        var record = new WorkflowExecutionLogRecord
        {
            Id = identityGenerator.GenerateId(),
            WorkflowInstanceId = instance.Id,
            WorkflowDefinitionId = instance.DefinitionId,
            WorkflowDefinitionVersionId = instance.DefinitionVersionId,
            WorkflowVersion = instance.Version,
            ActivityInstanceId = payload.LastActivityId ?? string.Empty,
            ActivityId = payload.LastActivityId ?? string.Empty,
            ActivityType = string.Empty,
            ActivityNodeId = payload.LastActivityNodeId ?? string.Empty,
            Timestamp = payload.InterruptedAt,
            EventName = WorkflowInterruptedPayload.WorkflowInterruptedEventName,
            Source = "Elsa.Workflows.Runtime.GracefulShutdown",
            Message = $"Workflow execution cycle was force-cancelled by the runtime ({payload.Reason}).",
            Payload = payload,
        };

        await store.AddAsync(record, cancellationToken);
    }
}
