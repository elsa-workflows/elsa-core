using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Reserved persisted metadata for dispatchers that commit an instance before delivering its first run.
/// </summary>
public static class WorkflowStartDelivery
{
    public const string IdProperty = "Elsa.Workflows.StartDelivery.Id";
    public const string TriggerActivityProperty = "Elsa.Workflows.StartDelivery.TriggerActivityId";

    /// <summary>
    /// Indicates whether the requested delivery owns the pending instance. A legacy message may
    /// complete an unmarked legacy instance during an upgrade.
    /// </summary>
    public static bool Matches(WorkflowState state, string? deliveryId)
    {
        return state.Properties.TryGetValue(IdProperty, out var stored)
            ? deliveryId != null && string.Equals(stored as string, deliveryId, StringComparison.Ordinal)
            : deliveryId == null;
    }
}
