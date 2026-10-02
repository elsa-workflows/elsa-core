using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Shared.Enums;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs;

/// The common tab for an activity.
public partial class CommonTab
{
    /// The activity.
    [Parameter] public JsonObject? Activity { get; set; }
    
    /// The activity descriptor.
    [Parameter] public ActivityDescriptor? ActivityDescriptor { get; set; }
    
    /// An event raised when the activity is updated.
    [Parameter] public Func<JsonObject, Task>? OnActivityUpdated { get; set; }
    [CascadingParameter] private IWorkspace? Workspace { get; set; }

    private bool IsTrigger => ActivityDescriptor?.Kind == ActivityKind.Trigger;
    private bool IsReadOnly => Workspace?.IsReadOnly == true;

    private async Task OnActivityIdChanged(string value)
    {
        Activity!.SetId(value);
        await RaiseActivityUpdated();
    }

    private async Task RaiseActivityUpdated()
    {
        if (OnActivityUpdated != null)
            await OnActivityUpdated(Activity!);
    }
    
    private async Task OnActivityNameChanged(string value)
    {
        Activity!.SetName(value);
        await RaiseActivityUpdated();
    }

    private async Task OnActivityDisplayTextChanged(string value)
    {
        Activity!.SetDisplayText(value);
        await RaiseActivityUpdated();
    }

    private async Task OnActivityDescriptionChanged(string value)
    {
        Activity!.SetDescription(value);
        await RaiseActivityUpdated();
    }

    private async Task OnShowDescriptionChanged(bool? value)
    {
        Activity!.SetShowDescription(value == true);
        await RaiseActivityUpdated();
    }

    private async Task OnCanStartWorkflowChanged(bool? value)
    {
        Activity!.SetCanStartWorkflow(value == true);
        await RaiseActivityUpdated();
    }
    
    private async Task OnMergeModeChanged(MergeMode value)
    {
        Activity!.SetMergeMode(value);
        await RaiseActivityUpdated();
    }
}