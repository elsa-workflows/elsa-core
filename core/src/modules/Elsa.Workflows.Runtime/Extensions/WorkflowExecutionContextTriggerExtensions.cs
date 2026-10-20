using System.Text.Json;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Reads what the runtime records about the stored trigger that started a workflow.
/// </summary>
public static class WorkflowExecutionContextTriggerExtensions
{
    /// <summary>
    /// The stimulus hash of the stored trigger that started this workflow instance, or <c>null</c> when it was not
    /// started from a stored trigger, or was started by a path that does not record it (a direct run with a trigger
    /// activity, or a dispatch queued before the runtime recorded it).
    /// </summary>
    /// <remarks>
    /// The value is the <see cref="Elsa.Workflows.Runtime.Entities.StoredTrigger.Hash"/> of the matched trigger, the
    /// same hash <see cref="IStimulusHasher"/> computes from the trigger's stimulus name and payload. It is kept for the
    /// lifetime of the instance, so it describes how the instance started, not how it was last resumed.
    /// </remarks>
    public static string? GetTriggerStimulusHash(this WorkflowExecutionContext context)
    {
        if (!context.Properties.TryGetValue(TriggerWorkflowProperties.StimulusHash, out var value))
        {
            return null;
        }

        // A property that went through a JSON hop (a scheduler's job data, for instance) can come back as an element.
        var hash = value switch
        {
            string text => text,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            _ => null
        };

        return string.IsNullOrWhiteSpace(hash) ? null : hash;
    }
}
