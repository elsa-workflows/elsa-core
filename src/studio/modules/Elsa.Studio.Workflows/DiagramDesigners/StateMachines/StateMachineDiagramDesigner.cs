using System.Text.Json.Nodes;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.UI.Contexts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Elsa.Studio.Workflows.DiagramDesigners.StateMachines;

/// <summary>
/// A diagram designer that displays a StateMachine activity.
/// </summary>
public class StateMachineDiagramDesigner(ILocalizer localizer) : IDiagramDesignerToolboxProvider
{
    private readonly Guid _id = Guid.NewGuid();
    private StateMachineDesignerWrapper? _designerWrapper;
    private JsonObject _rootActivity = [];

    /// <inheritdoc />
    public async Task LoadRootActivityAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStatsMap)
    {
        _rootActivity = activity;
        await InvokeDesignerActionAsync(x => x.LoadStateMachineAsync(activity, activityStatsMap));
    }

    /// <inheritdoc />
    public async Task UpdateActivityAsync(string id, JsonObject activity)
    {
        await InvokeDesignerActionAsync(x => x.UpdateActivityAsync(id, activity));
    }

    /// <inheritdoc />
    public async Task UpdateActivityStatsAsync(string id, ActivityStats stats)
    {
        await InvokeDesignerActionAsync(x => x.UpdateActivityStatsAsync(id, stats));
    }

    /// <inheritdoc />
    public async Task SelectActivityAsync(string id)
    {
        await InvokeDesignerActionAsync(x => x.SelectActivityAsync(id));
    }

    /// <inheritdoc />
    public async Task<JsonObject> ReadRootActivityAsync()
    {
        return _designerWrapper != null ? await _designerWrapper.ReadRootActivityAsync() : _rootActivity;
    }

    /// <inheritdoc />
    public RenderFragment DisplayDesigner(DisplayContext context)
    {
        var stateMachine = context.Activity;
        _rootActivity = stateMachine;
        var sequence = 0;

        return builder =>
        {
            builder.OpenComponent<StateMachineDesignerWrapper>(sequence++);
            builder.SetKey(_id);
            builder.AddAttribute(sequence++, nameof(StateMachineDesignerWrapper.StateMachine), stateMachine);
            builder.AddAttribute(sequence++, nameof(StateMachineDesignerWrapper.IsReadOnly), context.IsReadOnly);
            builder.AddAttribute(sequence++, nameof(StateMachineDesignerWrapper.ActivityStats), context.ActivityStats);
            builder.AddAttribute(sequence++, nameof(StateMachineDesignerWrapper.ActivitySelected), context.ActivitySelectedCallback);
            builder.AddAttribute(sequence++, nameof(StateMachineDesignerWrapper.ActivityDoubleClick), context.ActivityDoubleClickCallback);
            builder.AddAttribute(sequence++, nameof(StateMachineDesignerWrapper.GraphUpdated), context.GraphUpdatedCallback);
            builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (StateMachineDesignerWrapper)@ref);
            builder.CloseComponent();
        };
    }

    /// <inheritdoc />
    public IEnumerable<RenderFragment> GetToolboxItems(bool isReadOnly)
    {
        yield return DisplayToolboxItem(Icons.Material.Outlined.FitScreen, localizer["Zoom to fit the screen"], OnZoomToFitClicked);
        yield return DisplayToolboxItem(Icons.Material.Filled.FilterCenterFocus, localizer["Center"], OnCenterClicked);
    }

    private RenderFragment DisplayToolboxItem(string icon, string description, Func<Task> onClick)
    {
        return builder =>
        {
            builder.OpenComponent<MudTooltip>(0);
            builder.AddAttribute(1, nameof(MudTooltip.Text), description);
            builder.AddAttribute(2, nameof(MudTooltip.Delay), 500d);
            builder.AddAttribute(3, nameof(MudTooltip.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<MudIconButton>(0);
                childBuilder.AddAttribute(1, nameof(MudIconButton.Icon), icon);
                childBuilder.AddAttribute(2, nameof(MudIconButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, onClick));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private async Task InvokeDesignerActionAsync(Func<StateMachineDesignerWrapper, Task> action)
    {
        if (_designerWrapper != null)
            await action(_designerWrapper);
    }

    private Task OnZoomToFitClicked() => _designerWrapper != null ? _designerWrapper.ZoomToFitAsync() : Task.CompletedTask;
    private Task OnCenterClicked() => _designerWrapper != null ? _designerWrapper.CenterContentAsync() : Task.CompletedTask;
}
