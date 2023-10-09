using Elsa.Workflows.Runtime.Options;

namespace Elsa.Workflows.Runtime.Filters;

/// <summary>
/// A filter for finding workflows to trigger.
/// </summary>
/// <param name="ActivityTypeName">The activity type name to trigger workflows for.</param>
/// <param name="BookmarkPayload">The bookmark payload to trigger workflows for.</param>
/// <param name="Options">The options to use when triggering workflows.</param>
public record WorkflowsFilter(string ActivityTypeName, object BookmarkPayload, TriggerWorkflowsOptions Options);