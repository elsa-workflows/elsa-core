using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.UI.Args;
using Elsa.Studio.Workflows.UI.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.UI.Contexts;

/// <summary>
/// Represents a context for displaying an activity.
/// </summary>
/// <param name="Activity">The activity to display.</param>
/// <param name="ActivitySelectedCallback">A callback that is invoked when an activity is selected.</param>
/// <param name="ActivityEmbeddedPortSelectedCallback">A callback that is invoked when an embedded port is selected.</param>
/// <param name="GraphUpdatedCallback">A callback that is invoked when the graph is updated.</param>
/// <param name="IsReadOnly">Whether the activity is read-only.</param>
/// <param name="ActivityStats">A map of activity stats.</param>
public record DisplayContext(
    JsonObject Activity,
    EventCallback<JsonObject> ActivitySelectedCallback = default,
    EventCallback<JsonObject> ActivityUpdated = default,
    EventCallback<ActivityEmbeddedPortSelectedArgs> ActivityEmbeddedPortSelectedCallback = default,
    EventCallback<JsonObject> ActivityDoubleClickCallback = default,
    EventCallback GraphUpdatedCallback = default,
    bool IsReadOnly = false,
    IDictionary<string, ActivityStats>? ActivityStats = null)
{
    /// <summary>
    /// The workflow definition that the activity belongs to, if known. Diagram designers can use this to derive a
    /// proposed file name for exports instead of relying on the root activity's JSON, which does not reliably
    /// carry the workflow's name or version.
    /// </summary>
    public WorkflowDefinition? WorkflowDefinition { get; init; }
}
