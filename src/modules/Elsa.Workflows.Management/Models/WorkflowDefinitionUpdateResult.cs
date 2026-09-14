using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management.Models;

/// <summary>
/// The outcome of <see cref="IWorkflowDefinitionStore.TryUpdateLatestAsync"/>.
/// </summary>
public enum WorkflowDefinitionUpdateOutcome
{
    /// <summary>The update ran and the definition was saved.</summary>
    Updated,

    /// <summary>No definition matched the filter.</summary>
    NotFound,

    /// <summary>
    /// The loaded definition no longer matched what the caller expected — another writer saved first.
    /// </summary>
    Conflict
}

/// <summary>
/// The result of a compare-and-swap update of the latest workflow definition.
/// </summary>
/// <param name="Outcome">Whether the swap saved, found nothing, or lost the race.</param>
/// <param name="Definition">The saved definition when <see cref="Outcome"/> is <see cref="WorkflowDefinitionUpdateOutcome.Updated"/>.</param>
public sealed record WorkflowDefinitionUpdateResult(WorkflowDefinitionUpdateOutcome Outcome, WorkflowDefinition? Definition = null)
{
    /// <summary>The update ran and <paramref name="definition"/> is what was saved.</summary>
    public static WorkflowDefinitionUpdateResult Updated(WorkflowDefinition definition) =>
        new(WorkflowDefinitionUpdateOutcome.Updated, definition);

    /// <summary>No definition matched the filter.</summary>
    public static WorkflowDefinitionUpdateResult NotFound() => new(WorkflowDefinitionUpdateOutcome.NotFound);

    /// <summary>Another writer saved first; the caller's expected snapshot is no longer current.</summary>
    public static WorkflowDefinitionUpdateResult Conflict() => new(WorkflowDefinitionUpdateOutcome.Conflict);
}
