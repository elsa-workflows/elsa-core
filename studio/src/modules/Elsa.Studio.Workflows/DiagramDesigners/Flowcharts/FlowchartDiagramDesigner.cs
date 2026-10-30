using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Designer.Options;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.UI.Contexts;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using MudBlazor;

namespace Elsa.Studio.Workflows.DiagramDesigners.Flowcharts;

/// <summary>
/// A diagram designer that displays a flowchart.
/// </summary>
public class FlowchartDiagramDesigner(ILocalizer localizer, IDialogService dialogService, IOptions<DesignerOptions> designerOptions) : IDiagramDesignerToolboxProvider
{
    private const string DefaultFileName = "flowchart";

    private FlowchartDesignerWrapper? _designerWrapper;
    private readonly Guid _id = Guid.NewGuid();
    private WorkflowDefinition? _workflowDefinition;

    /// <inheritdoc />
    public async Task LoadRootActivityAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStatsMap)
    {
        await InvokeDesignerActionAsync(x => x.LoadFlowchartAsync(activity, activityStatsMap));
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
        var flowchart = context.Activity;
        var sequence = 0;

        _workflowDefinition = context.WorkflowDefinition;

        return builder =>
        {
            builder.OpenComponent<FlowchartDesignerWrapper>(sequence++);
            builder.SetKey(_id);
            builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.Flowchart),  flowchart);
            builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.IsReadOnly), context.IsReadOnly);
            builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivityStats), context.ActivityStats);
            builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivitySelected), context.ActivitySelectedCallback);
            builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivityUpdated), context.ActivityUpdated);
            builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivityEmbeddedPortSelected), context.ActivityEmbeddedPortSelectedCallback);
            builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivityDoubleClick), context.ActivityDoubleClickCallback);
            builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.GraphUpdated), context.GraphUpdatedCallback);
            builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (FlowchartDesignerWrapper)@ref);

            builder.CloseComponent();
        };
    }

    /// <inheritdoc />
    public IEnumerable<RenderFragment> GetToolboxItems(bool isReadonly)
    {
        yield return DiagramDesignerToolbox.DisplayToolboxItem(localizer["Zoom to fit"], Icons.Material.Outlined.FitScreen, localizer["Zoom to fit the screen"], OnZoomToFitClicked);
        yield return DiagramDesignerToolbox.DisplayToolboxItem(localizer["Center"], Icons.Material.Filled.FilterCenterFocus, localizer["Center"], OnCenterClicked);
        yield return DiagramDesignerToolbox.DisplayToolboxItem(localizer["Auto layout"], Icons.Material.Outlined.AutoAwesomeMosaic, localizer["Auto layout"], OnAutoLayoutClicked);

        // Exporting is available in both read-only and editable mode, but only the X6 designer can produce an image.
        if (!designerOptions.Value.UseReactFlow)
            yield return DiagramDesignerToolbox.DisplayToolboxItem(localizer["Export"], Icons.Material.Outlined.Download, localizer["Export as image"], OnExportClicked);
    }

    private async Task InvokeDesignerActionAsync(Func<FlowchartDesignerWrapper, Task> action)
    {
        if (_designerWrapper != null && action != null)
            await action(_designerWrapper);
    }

    private Task OnZoomToFitClicked() => _designerWrapper != null ? _designerWrapper.ZoomToFitAsync() : Task.CompletedTask;
    private Task OnCenterClicked() => _designerWrapper != null ? _designerWrapper!.CenterContentAsync() : Task.CompletedTask;
    private Task OnAutoLayoutClicked() => _designerWrapper != null ? _designerWrapper!.AutoLayoutAsync() : Task.CompletedTask;

    private async Task OnExportClicked()
    {
        if (_designerWrapper == null)
            return;

        var parameters = new DialogParameters<ExportFlowchartDialog>
        {
            { x => x.FileName, GetDefaultFileName(_workflowDefinition) }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        var dialog = await dialogService.ShowAsync<ExportFlowchartDialog>(localizer["Export flowchart"], parameters, options);
        var result = await dialog.Result;

        if ((result?.Canceled ?? true) || result.Data is not ExportGraphOptions exportOptions)
            return;

        await _designerWrapper.ExportGraphAsync(exportOptions);
    }

    /// <summary>
    /// Derives the file name to propose for an export from the workflow definition's name, suffixed with its
    /// version when the definition carries one. The root activity's JSON is not used, because for a real workflow
    /// its root activity carries no name and its version is the activity type's version, not the workflow's.
    /// Characters that the host platform does not allow in a file name are replaced with an underscore; the
    /// browser applies its own sanitization to the download name on top of this.
    /// </summary>
    internal static string GetDefaultFileName(WorkflowDefinition? workflowDefinition)
    {
        var name = workflowDefinition?.Name?.Trim();
        var invalidChars = Path.GetInvalidFileNameChars();
        var fileName = string.IsNullOrEmpty(name)
            ? DefaultFileName
            : new string(name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
        var version = workflowDefinition?.Version ?? 0;

        return version > 0 ? $"{fileName}_v{version}" : fileName;
    }
}