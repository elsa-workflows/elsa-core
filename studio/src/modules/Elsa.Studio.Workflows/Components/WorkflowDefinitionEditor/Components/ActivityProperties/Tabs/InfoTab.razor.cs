using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs;

/// <summary>
/// The info tab for an activity.
/// </summary>
public partial class InfoTab
{
    /// <summary>
    /// The activity descriptor.
    /// </summary>
    [Parameter] public ActivityDescriptor ActivityDescriptor { get; set; } = null!;
    
    /// <summary>
    /// The activity.
    /// </summary>
    [Parameter] public JsonObject Activity { get; set; } = null!;

    private DataPanelModel ActivityInfo { get; } = new();

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ActivityDescriptor.ConstructionProperties.TryGetValue("WorkflowDefinitionId", out var link);
        ActivityInfo.Clear();
        ActivityInfo.Add("ID", Activity.GetId());
        ActivityInfo.Add("Node ID", Activity.GetNodeId());
        ActivityInfo.Add(Localizer["Type"], ActivityDescriptor.TypeName, link == null ? null : $"/workflows/definitions/{link}/edit");
        ActivityInfo.Add(Localizer["Description"], ActivityDescriptor.Description);
        ActivityInfo.Add(Localizer["Version"], ActivityDescriptor.Version.ToString());
    }
}