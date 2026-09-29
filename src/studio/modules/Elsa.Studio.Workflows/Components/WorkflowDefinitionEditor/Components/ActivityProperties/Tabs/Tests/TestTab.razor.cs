using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.Tests;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs.Tests;

/// <summary>
/// The info tab for an activity.
/// </summary>
public partial class TestTab
{
    /// <summary>
    /// The workflow definition.
    /// </summary>
    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = null!;
    
    /// <summary>
    /// The activity descriptor.
    /// </summary>
    [Parameter] public ActivityDescriptor ActivityDescriptor { get; set; } = null!;
    
    /// <summary>
    /// The activity.
    /// </summary>
    [Parameter] public JsonObject Activity { get; set; } = null!;
    
    /// <summary>
    /// Callback event for test result changes.
    /// </summary>
    [Parameter] public EventCallback<ActivityStatus> OnTestResultChanged { get; set; }

    [Inject] private IBackendApiClientProvider BackendApiClientProvider { get; set; } = null!;
    
    private DataPanelModel Status { get; set; } = [];
    private DataPanelModel ActivityState { get; set; } = [];
    private DataPanelModel Outcomes { get; set; } = [];
    private DataPanelModel Output { get; set; } = [];
    private DataPanelModel Fault { get; set; } = [];

    private bool HasRun { get; set; }
    private bool IsRunning { get; set; }

    private async Task OnTestRunClick()
    {
        var testsApi = await BackendApiClientProvider.GetApiAsync<ITestsApi>();
        var request = new TestActivityRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId(WorkflowDefinition.Id),
            ActivityHandle = ActivityHandle.FromActivityNodeId(Activity.GetNodeId())
        };
        IsRunning = true;
        HasRun = true;
        StateHasChanged();
        var response = await testsApi.TestActivityAsync(request);
        UpdateDataModels(response);
        IsRunning = false;
        StateHasChanged();
    }
    
    private void UpdateDataModels(TestActivityResponse response)
    {
        var status = new DataPanelModel
        {
            { "Status", response.Status.ToString() }
        };
        OnTestResultChanged.InvokeAsync(response.Status);

        var activityState = response.ActivityState
            .Where(x => !x.Key.StartsWith("_"))
            .Select(x => new DataPanelItem(x.Key, x.Value?.ToString()))
            .ToDataPanelModel();

        var outcomesData = response.Payload?.TryGetValue("Outcomes", out var outcomesValue) == true
            ? new DataPanelModel { new DataPanelItem("Outcomes", outcomesValue!.ToString()!) }
            : null;

        var outputData = new DataPanelModel();

        if (response.Outputs != null)
            foreach (var (key, value) in response.Outputs)
                outputData.Add(key, value?.ToString());

        Status = status;
        ActivityState = activityState;
        Outcomes = outcomesData ?? [];
        Output = outputData;

        if (response.Exception is not null)
        {
            var fault = new DataPanelModel
            {
                { "Message", response.Exception?.Message },
                { "StackTrace", response.Exception?.StackTrace }
            };
            Fault = fault;
        }
    }
}
