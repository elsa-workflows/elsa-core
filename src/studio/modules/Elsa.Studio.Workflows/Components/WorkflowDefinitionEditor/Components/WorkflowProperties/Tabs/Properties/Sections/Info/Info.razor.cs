using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Properties.
    Sections.Info;

/// <summary>
/// Represents the info.
/// </summary>
public partial class Info
{
    private DataPanelModel _workflowInfo = new ();
    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _workflowInfo =
        [
            new DataPanelItem(Localizer["Definition ID"], WorkflowDefinition.DefinitionId),
            new DataPanelItem(Localizer["Version ID"], WorkflowDefinition.Id),
            new DataPanelItem(Localizer["Version"], WorkflowDefinition.Version.ToString()),
            new DataPanelItem(Localizer["Status"], WorkflowDefinition.IsPublished ? Localizer["Published"] : Localizer["Draft"]),
            new DataPanelItem(Localizer["Readonly"], WorkflowDefinition.IsReadonly ? Localizer["Yes"] : Localizer["No"])
        ];
    }
}