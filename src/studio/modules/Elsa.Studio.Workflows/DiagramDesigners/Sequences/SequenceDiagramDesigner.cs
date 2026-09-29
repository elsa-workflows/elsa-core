using System.Text.Json.Nodes;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.UI.Contexts;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Elsa.Studio.Workflows.DiagramDesigners.Sequences;

/// <summary>
/// A diagram designer that displays a Sequence activity.
/// </summary>
public class SequenceDiagramDesigner(ILocalizer localizer) : IDiagramDesignerToolboxProvider
{
    private SequenceDesignerWrapper? _designerWrapper;
    private readonly Guid _id = Guid.NewGuid();

    /// <inheritdoc />
    public async Task LoadRootActivityAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStatsMap)
    {
        await InvokeDesignerActionAsync(x => x.LoadSequenceAsync(activity, activityStatsMap));
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
        return await _designerWrapper!.ReadRootActivityAsync();
    }

    /// <inheritdoc />
    public RenderFragment DisplayDesigner(DisplayContext context)
    {
        var sequence = 0;

        return builder =>
        {
            builder.OpenComponent<SequenceDesignerWrapper>(sequence++);
            builder.SetKey(_id);
            builder.AddAttribute(sequence++, nameof(SequenceDesignerWrapper.Sequence), context.Activity);
            builder.AddAttribute(sequence++, nameof(SequenceDesignerWrapper.IsReadOnly), context.IsReadOnly);
            builder.AddAttribute(sequence++, nameof(SequenceDesignerWrapper.ActivityStats), context.ActivityStats);
            builder.AddAttribute(sequence++, nameof(SequenceDesignerWrapper.ActivitySelected), context.ActivitySelectedCallback);
            builder.AddAttribute(sequence++, nameof(SequenceDesignerWrapper.ActivityUpdated), context.ActivityUpdated);
            builder.AddAttribute(sequence++, nameof(SequenceDesignerWrapper.ActivityEmbeddedPortSelected), context.ActivityEmbeddedPortSelectedCallback);
            builder.AddAttribute(sequence++, nameof(SequenceDesignerWrapper.ActivityDoubleClick), context.ActivityDoubleClickCallback);
            builder.AddAttribute(sequence++, nameof(SequenceDesignerWrapper.GraphUpdated), context.GraphUpdatedCallback);
            builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (SequenceDesignerWrapper)@ref);
            builder.CloseComponent();
        };
    }

    /// <inheritdoc />
    public IEnumerable<RenderFragment> GetToolboxItems(bool isReadonly)
    {
        if (!isReadonly)
        {
            yield return DisplayToolboxItem(localizer["Vertical layout"], Icons.Material.Outlined.SwapVert, localizer["Use vertical Sequence layout"], OnVerticalLayoutClicked);
            yield return DisplayToolboxItem(localizer["Horizontal layout"], Icons.Material.Outlined.SwapHoriz, localizer["Use horizontal Sequence layout"], OnHorizontalLayoutClicked);
            yield return DisplayToolboxItem(localizer["Move earlier"], Icons.Material.Outlined.KeyboardArrowUp, localizer["Move selected activity earlier"], OnMoveEarlierClicked);
            yield return DisplayToolboxItem(localizer["Move later"], Icons.Material.Outlined.KeyboardArrowDown, localizer["Move selected activity later"], OnMoveLaterClicked);
        }

        yield return DisplayToolboxItem(localizer["Zoom to fit"], Icons.Material.Outlined.FitScreen, localizer["Zoom to fit the screen"], OnZoomToFitClicked);
        yield return DisplayToolboxItem(localizer["Center"], Icons.Material.Filled.FilterCenterFocus, localizer["Center"], OnCenterClicked);
        yield return DisplayToolboxItem(localizer["Auto layout"], Icons.Material.Outlined.AutoAwesomeMosaic, localizer["Auto layout"], OnAutoLayoutClicked);
    }

    private RenderFragment DisplayToolboxItem(string title, string icon, string description, Func<Task> onClick)
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

    private async Task InvokeDesignerActionAsync(Func<SequenceDesignerWrapper, Task> action)
    {
        if (_designerWrapper != null)
            await action(_designerWrapper);
    }

    private Task OnZoomToFitClicked() => _designerWrapper != null ? _designerWrapper.ZoomToFitAsync() : Task.CompletedTask;
    private Task OnCenterClicked() => _designerWrapper != null ? _designerWrapper.CenterContentAsync() : Task.CompletedTask;
    private Task OnAutoLayoutClicked() => _designerWrapper != null ? _designerWrapper.AutoLayoutAsync() : Task.CompletedTask;
    private Task OnVerticalLayoutClicked() => _designerWrapper != null ? _designerWrapper.SetLayoutOrientationAsync("vertical") : Task.CompletedTask;
    private Task OnHorizontalLayoutClicked() => _designerWrapper != null ? _designerWrapper.SetLayoutOrientationAsync("horizontal") : Task.CompletedTask;
    private Task OnMoveEarlierClicked() => _designerWrapper != null ? _designerWrapper.MoveSelectedActivityAsync(-1) : Task.CompletedTask;
    private Task OnMoveLaterClicked() => _designerWrapper != null ? _designerWrapper.MoveSelectedActivityAsync(1) : Task.CompletedTask;
}
