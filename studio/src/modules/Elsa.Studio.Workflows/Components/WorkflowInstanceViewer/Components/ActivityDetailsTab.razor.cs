using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Humanizer;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;

/// Displays the details of an activity.
public partial class ActivityDetailsTab
{
    /// The height of the visible pane.
    [Parameter] public int VisiblePaneHeight { get; set; }

    /// The activity to display details for.
    [Parameter] public JsonObject Activity { get; set; } = null!;

    /// The latest activity execution record. Used for displaying the last state of the activity.
    [Parameter] public ActivityExecutionRecord? LastActivityExecution { get; set; }

    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = null!;

    private DataPanelModel ActivityInfo { get; set; } = new();
    private DataPanelModel ActivityData { get; set; } = new();
    private DataPanelModel OutcomesData { get; set; } = new();
    private DataPanelModel OutputData { get; set; } = new();
    private DataPanelModel ExceptionData { get; set; } = new();
    private DataPanelModel ResilienceStrategyData { get; set; } = new();

    /// Refreshes the component.
    public void Refresh()
    {
        CreateDataModels();
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        CreateDataModels();
    }

    private void CreateDataModels()
    {
        ClearDataModels();

        var activity = Activity;
        var activityDescriptor = ActivityRegistry.Find(activity.GetTypeName(), activity.GetVersion());
        if (activityDescriptor is null)
            return;
        var activityId = activity.GetId();
        var activityNodeId = activity.GetNodeId();
        var activityName = activity.GetName();
        var activityType = activity.GetTypeName();
        var activityDescription = activity.GetDescription();
        var execution = LastActivityExecution;
        var activityVersion = activity.GetVersion();
        var exception = execution?.Exception;
        var workflowDefinitionId = activity.GetIsWorkflowDefinitionActivity() ? activity.GetWorkflowDefinitionId() : null;
        var props = execution?.Properties;
        var resilienceStrategy = props != null && props.TryGetValue("ResilienceStrategy", out var resilienceStrategyModel) && resilienceStrategyModel is JsonElement resilienceStrategyElement ? resilienceStrategyElement : default(JsonElement?);

        var activityInfo = new DataPanelModel
        {
            new DataPanelItem("ID", activityId),
            new DataPanelItem("Node ID", activityNodeId),
            new DataPanelItem(Localizer["Name"], activityName),
            new DataPanelItem(Localizer["Type"], activityType,
                string.IsNullOrWhiteSpace(workflowDefinitionId)
                    ? null
                    : $"/workflows/definitions/{workflowDefinitionId}/edit"),
            new DataPanelItem(Localizer["Description"], activityDescription),
            new DataPanelItem(Localizer["Version"], activityVersion.ToString())
        };

        var outcomesData = new DataPanelModel();
        var outputData = new DataPanelModel();

        if (execution != null)
        {
            activityInfo.Add(Localizer["Status"], execution.Status.ToString());
            activityInfo.Add(Localizer["Instance ID"], execution.Id);

            if (execution.Payload != null)
                if (execution.Payload.TryGetValue("Outcomes", out var outcomes))
                    outcomesData.Add("Outcomes", outcomes?.ToString());

            var outputDescriptors = activityDescriptor.Outputs;
            var outputs = execution.Outputs;

            foreach (var outputDescriptor in outputDescriptors)
            {
                var outputName = outputDescriptor.Name;
                if (string.IsNullOrWhiteSpace(outputName))
                    continue;

                var outputValue = outputs != null
                    ? outputs.TryGetValue(outputName, out var value) ? value : null
                    : null;
                outputData.Add(outputName, outputValue?.ToString());
            }
        }
        else
        {
            activityInfo.Add("Status", Localizer["Not executed"]);
        }

        var exceptionData = new DataPanelModel();

        if (exception != null)
        {
            exceptionData.Add("Message", exception.Message);
            exceptionData.Add("InnerException", exception.InnerException != null
                ? exception.InnerException.Type + ": " + exception.InnerException.Message
                : null);
            exceptionData.Add("StackTrace", exception.StackTrace);
        }

        var activityStateData = new DataPanelModel();
        var activityState = execution?.ActivityState;

        if (activityState != null)
        {
            foreach (var inputDescriptor in activityDescriptor.Inputs)
            {
                var inputValue = activityState.TryGetValue(inputDescriptor.Name, out var value) ? value : null;
                activityStateData.Add(inputDescriptor.Name, inputValue?.ToString());
            }
        }

        var resilienceStrategyData = new DataPanelModel();

        if (resilienceStrategy != null)
        {
            var resilienceStrategyJson = JsonSerializer.Serialize(resilienceStrategy.Value, JsonSerializerOptions.Default);
            var dict = JsonSerializer.Deserialize<IDictionary<string, object>>(resilienceStrategyJson)!;
            var type = "Unknown";

            if (dict.ContainsKey("$type"))
            {
                type = dict["$type"].ToString();
                dict.Remove("$type");
            }

            var panelItems = dict.Select(x => new DataPanelItem(x.Key.Pascalize(), x.Value.ToString()));
            resilienceStrategyData.Add("Type", type);
            resilienceStrategyData.AddRange(panelItems);
        }

        ActivityInfo = activityInfo;
        ActivityData = activityStateData;
        OutcomesData = outcomesData;
        OutputData = outputData;
        ExceptionData = exceptionData;
        ResilienceStrategyData = resilienceStrategyData;
    }

    private void ClearDataModels()
    {
        ActivityInfo = new();
        ActivityData = new();
        OutcomesData = new();
        OutputData = new();
        ExceptionData = new();
        ResilienceStrategyData = new();
    }
}
