using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Components;
using Elsa.Studio.Workflows.Designer.Interop;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Helpers;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Designer.Components.ActivityWrappers;

/// <summary>
/// Represents the embedded activity wrapper base.
/// </summary>
public abstract class EmbeddedActivityWrapperBase : StudioComponentBase
{
    [Parameter] public string? ElementId { get; set; }
    [Parameter] public string ActivityId { get; set; } = null!;
    [Parameter] public JsonObject Activity { get; set; } = null!;
    [Parameter] public bool IsSelected { get; set; }

    [Inject] DesignerJsInterop DesignerInterop { get; set; } = null!;
    [Inject] IActivityRegistry ActivityRegistry { get; set; } = null!;
    [Inject] IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = null!;
    [Inject] IServiceProvider ServiceProvider { get; set; } = null!;

    /// <summary>
    /// Provides the can start workflow.
    /// </summary>
    protected bool CanStartWorkflow => Activity.GetCanStartWorkflow() == true;
    /// <summary>
    /// Gets or sets the label.
    /// </summary>
    protected string Label { get; private set; } = null!;
    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    protected string Description { get; private set; } = null!;
    /// <summary>
    /// Indicates whether show description.
    /// </summary>
    protected bool ShowDescription { get; private set; }
    /// <summary>
    /// Gets or sets the color.
    /// </summary>
    protected string Color  { get; private set; } = null!;
    /// <summary>
    /// Gets or sets the icon.
    /// </summary>
    protected string? Icon  { get; private set; }
    /// <summary>
    /// Gets or sets the activity descriptor.
    /// </summary>
    protected ActivityDescriptor ActivityDescriptor  { get; private set; } = null!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await ActivityRegistry.EnsureLoadedAsync();
        
        var activity = Activity;
        var activityType = activity.GetTypeName();
        var activityVersion = activity.GetVersion();
        var descriptor = (ActivityRegistry.Find(activityType, activityVersion) ?? ActivityRegistry.Find("Elsa.UnknownActivity"))!;
        var activityDescription = activity.GetDescription()?.Trim();
        var displaySettings = ActivityDisplaySettingsRegistry.GetSettings(activityType);

        Label = ActivityLabelResolver.Resolve(activity.GetDisplayText(), activity.GetName(), descriptor.DisplayName ?? descriptor.Name);
        Description = !string.IsNullOrEmpty(activityDescription) ? activityDescription : descriptor.Description ?? string.Empty;
        ShowDescription = activity.GetShowDescription() == true;
        Color = displaySettings.Color;
        Icon = displaySettings.Icon;
        ActivityDescriptor = descriptor;
    }
}