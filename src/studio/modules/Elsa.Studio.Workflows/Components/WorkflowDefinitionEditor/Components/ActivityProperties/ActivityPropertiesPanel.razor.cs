using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs.Outputs.Components;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs.Tests;
using Elsa.Studio.Workflows.Domain.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties;

/// <summary>
/// Renders the properties of an activity.
/// </summary>
public partial class ActivityPropertiesPanel
{
    private bool _isInitialized;

    /// <summary>
    /// Gets or sets the workflow definition.
    /// </summary>
    [Parameter]
    public WorkflowDefinition? WorkflowDefinition { get; set; }

    /// <summary>
    /// Gets or sets the activity.
    /// </summary>
    [Parameter]
    public JsonObject? Activity { get; set; }

    /// <summary>
    /// Gets or sets the activity descriptor.
    /// </summary>
    [Parameter]
    public ActivityDescriptor? ActivityDescriptor
    {
        get => field;
        set
        {
            if (field != value)
                TestResultStatus = ActivityStatus.Canceled;

            field = value;
        }
    }

    /// <summary>
    /// Gets or sets a callback that is invoked when the activity is updated.
    /// </summary>
    [Parameter]
    public Func<JsonObject, Task>? OnActivityUpdated { get; set; }

    /// <summary>
    /// Gets or sets the visible pane height.
    /// </summary>
    [Parameter]
    public int VisiblePaneHeight { get; set; }

    [Inject] private IExpressionService ExpressionService { get; set; } = null!;
    [Inject] private IRemoteFeatureProvider RemoteFeatureProvider { get; set; } = null!;
    [Inject] private IActivityTabRegistry ActivityTabRegistry { get; set; } = null!;
    private IEnumerable<IActivityTab> Tabs { get; set; } = Array.Empty<IActivityTab>();

    private IDictionary<string, object?> ActivityTabAttributes => new { WorkflowDefinition, Activity, ActivityDescriptor, OnActivityUpdated, VisiblePaneHeight }.ToDictionary();
    private ExpressionDescriptorProvider ExpressionDescriptorProvider { get; set; } = new();
    private bool IsResilienceEnabled { get; set; }
    private bool IsResilientActivity => ActivityDescriptor?.CustomProperties.TryGetValue("Resilient", out var resilientObj) == true && resilientObj.ConvertTo<bool>();
    private bool DisplayResilienceTab => IsResilienceEnabled && IsResilientActivity;
    private ActivityStatus TestResultStatus { get; set; } = ActivityStatus.Canceled;
    private Color TestIconColor => TestResultStatus switch
    {
        ActivityStatus.Running => Color.Warning,
        ActivityStatus.Completed => Color.Success,
        ActivityStatus.Canceled => Color.Default,
        ActivityStatus.Faulted => Color.Error,
        _ => Color.Error,
    };

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var expressionDescriptors = await ExpressionService.ListDescriptorsAsync();
        IsResilienceEnabled = await RemoteFeatureProvider.IsEnabledAsync("Elsa.Resilience");
        ExpressionDescriptorProvider = new();
        ExpressionDescriptorProvider.AddRange(expressionDescriptors);
        Tabs = CreateBuiltInTabs().Concat(ActivityTabRegistry.List()).OrderBy(x => x.Order).ToList();
        _isInitialized = true;
    }

    /// <summary>
    /// Updates the test result status when the tests status changes.
    /// </summary>
    /// <param name="status">The new activity status to set as the test result status.</param>
    private void OnTestResultChanged(ActivityStatus status) => TestResultStatus = status;

    /// <summary>
    /// Handles changes to the active panel index.
    /// </summary>
    /// <param name="newIndex">The new index of the active panel.</param>
    private void OnActivePanelIndexChanged(int newIndex) => TestResultStatus = ActivityStatus.Canceled;

    private bool IsWorkflowAsActivity => ActivityDescriptor != null && ActivityDescriptor.CustomProperties.TryGetValue("RootType", out var value) && value.ConvertTo<string>() == "WorkflowDefinitionActivity";
    private bool IsTaskActivity => ActivityDescriptor?.Kind == ActivityKind.Task;

    private IEnumerable<IActivityTab> CreateBuiltInTabs() =>
    [
        new ActivityTab(Localizer["Input"], 0, _ => RenderInputsTab())
        {
            Icon = Icons.Material.Outlined.Input,
            WrapInScrollableWell = false,
            VisibilityPredicate = _ => _isInitialized,
        },
        new ActivityTab(Localizer["Output"], 1, _ => RenderOutputsTab())
        {
            Icon = Icons.Material.Outlined.Output,
            WrapInScrollableWell = false,
            VisibilityPredicate = _ => _isInitialized,
        },
        new ActivityTab(Localizer["Common"], 2, _ => RenderCommonTab())
        {
            Icon = Icons.Material.Outlined.Notes,
            WrapInScrollableWell = false,
        },
        new ActivityTab(Localizer["Test"], 3, _ => RenderTestTab())
        {
            Icon = ElsaStudioIcons.Tabler.Flask,
            WrapInScrollableWell = false,
            IconColorProvider = _ => TestIconColor,
        },
        new ActivityTab(Localizer["Commit Strategy"], 4, _ => RenderCommitStrategyTab())
        {
            Icon = Icons.Material.Outlined.Commit,
            WrapInScrollableWell = false,
        },
        new ActivityTab(Localizer["Task"], 5, _ => RenderTaskTab())
        {
            Icon = Icons.Material.Outlined.HorizontalSplit,
            WrapInScrollableWell = false,
            VisibilityPredicate = _ => IsTaskActivity,
        },
        new ActivityTab(Localizer["Log"], 6, _ => RenderLogTab())
        {
            Icon = Icons.Material.Outlined.Assignment,
            WrapInScrollableWell = false,
        },
        new ActivityTab(Localizer["Info"], 7, _ => RenderInfoTab())
        {
            Icon = Icons.Material.Outlined.Info,
            WrapInScrollableWell = false,
        },
        new ActivityTab(Localizer["Version"], 8, _ => RenderVersionTab())
        {
            Icon = Icons.Material.Outlined.Numbers,
            WrapInScrollableWell = false,
            VisibilityPredicate = _ => IsWorkflowAsActivity,
        },
        new ActivityTab(Localizer["Resilience"], 9, _ => RenderResilienceTab())
        {
            Icon = Icons.Material.Outlined.RestartAlt,
            WrapInScrollableWell = false,
            VisibilityPredicate = _ => DisplayResilienceTab,
        },
    ];

    private RenderFragment RenderInputsTab() => ActivityDescriptor?.Inputs.Any(x => x.IsBrowsable != false) == true
        ? RenderScrollableComponent<InputsTab>(
            (nameof(InputsTab.WorkflowDefinition), WorkflowDefinition),
            (nameof(InputsTab.Activity), Activity),
            (nameof(InputsTab.ActivityDescriptor), ActivityDescriptor),
            (nameof(InputsTab.OnActivityUpdated), OnActivityUpdated))
        : RenderAlertInWell(Localizer["This activity does not have any input properties."]);

    private RenderFragment RenderOutputsTab() => ActivityDescriptor?.Outputs.Any(x => x.IsBrowsable != false) == true && WorkflowDefinition != null && Activity != null
        ? RenderScrollableComponent<OutputsTab>(
            (nameof(OutputsTab.WorkflowDefinition), WorkflowDefinition),
            (nameof(OutputsTab.Activity), Activity),
            (nameof(OutputsTab.ActivityDescriptor), ActivityDescriptor),
            (nameof(OutputsTab.OnActivityUpdated), OnActivityUpdated))
        : RenderAlertInWell(Localizer["This activity does not have any output properties."]);

    private RenderFragment RenderCommonTab() => Activity != null && ActivityDescriptor != null
        ? RenderScrollableComponent<CommonTab>(
            (nameof(CommonTab.Activity), Activity),
            (nameof(CommonTab.ActivityDescriptor), ActivityDescriptor),
            (nameof(CommonTab.OnActivityUpdated), OnActivityUpdated))
        : RenderAlertInWell(Localizer["This activity does not have any common properties."]);

    private RenderFragment RenderTestTab() => WorkflowDefinition != null && ActivityDescriptor != null && Activity != null
        ? RenderScrollableComponent<TestTab>(
            (nameof(TestTab.WorkflowDefinition), WorkflowDefinition),
            (nameof(TestTab.ActivityDescriptor), ActivityDescriptor),
            (nameof(TestTab.Activity), Activity),
            (nameof(TestTab.OnTestResultChanged), EventCallback.Factory.Create<ActivityStatus>(this, OnTestResultChanged)))
        : EmptyContent();

    private RenderFragment RenderCommitStrategyTab() => Activity != null
        ? RenderScrollableComponent<CommitStrategyTab>(
            (nameof(CommitStrategyTab.Activity), Activity),
            (nameof(CommitStrategyTab.OnActivityUpdated), OnActivityUpdated))
        : RenderAlertInWell(Localizer["This activity does not have a commit strategy."]);

    private RenderFragment RenderTaskTab() => RenderScrollableComponent<TaskTab>(
        (nameof(TaskTab.Activity), Activity),
        (nameof(TaskTab.ActivityDescriptor), ActivityDescriptor),
        (nameof(TaskTab.OnActivityUpdated), OnActivityUpdated));

    private RenderFragment RenderLogTab() => RenderScrollableComponent<LogPersistenceTab>(
        (nameof(LogPersistenceTab.WorkflowDefinition), WorkflowDefinition),
        (nameof(LogPersistenceTab.Activity), Activity),
        (nameof(LogPersistenceTab.ActivityDescriptor), ActivityDescriptor),
        (nameof(LogPersistenceTab.OnActivityUpdated), OnActivityUpdated));

    private RenderFragment RenderInfoTab() => ActivityDescriptor != null
        ? RenderScrollableComponent<InfoTab>(
            (nameof(InfoTab.ActivityDescriptor), ActivityDescriptor),
            (nameof(InfoTab.Activity), Activity))
        : EmptyContent();

    private RenderFragment RenderVersionTab() => RenderScrollableComponent<VersionTab>(
        (nameof(VersionTab.Activity), Activity),
        (nameof(VersionTab.ActivityDescriptor), ActivityDescriptor),
        (nameof(VersionTab.OnActivityUpdated), OnActivityUpdated));

    private RenderFragment RenderResilienceTab() => RenderScrollableComponent<ResilienceTab>(
        (nameof(ResilienceTab.WorkflowDefinition), WorkflowDefinition),
        (nameof(ResilienceTab.Activity), Activity),
        (nameof(ResilienceTab.ActivityDescriptor), ActivityDescriptor),
        (nameof(ResilienceTab.OnActivityUpdated), OnActivityUpdated));

    private RenderFragment RenderScrollableComponent<TComponent>(params (string Name, object? Value)[] parameters) where TComponent : IComponent => RenderScrollableWell(RenderComponent<TComponent>(parameters));

    private RenderFragment RenderScrollableWell(RenderFragment childContent) => builder =>
    {
        var sequence = 0;
        builder.OpenComponent<ScrollableWell>(sequence++);
        builder.AddAttribute(sequence++, nameof(ScrollableWell.MaxHeight), VisiblePaneHeight);
        builder.AddAttribute(sequence++, nameof(ScrollableWell.ChildContent), childContent);
        builder.CloseComponent();
    };

    private RenderFragment RenderWell(RenderFragment childContent) => builder =>
    {
        var sequence = 0;
        builder.OpenComponent<Well>(sequence++);
        builder.AddAttribute(sequence++, nameof(Well.ChildContent), childContent);
        builder.CloseComponent();
    };

    private RenderFragment RenderAlertInWell(string message) => RenderWell(RenderAlert(message));

    private static RenderFragment RenderAlert(string message) => builder =>
    {
        var sequence = 0;
        builder.OpenComponent<MudAlert>(sequence++);
        builder.AddAttribute(sequence++, nameof(MudAlert.Severity), Severity.Normal);
        builder.AddAttribute(sequence++, nameof(MudAlert.Variant), MudBlazor.Variant.Text);
        builder.AddAttribute(sequence++, nameof(MudAlert.ChildContent), (RenderFragment)(contentBuilder => contentBuilder.AddContent(0, message)));
        builder.CloseComponent();
    };

    private static RenderFragment EmptyContent() => _ => { };

    private static RenderFragment RenderComponent<TComponent>(params (string Name, object? Value)[] parameters) where TComponent : IComponent => builder =>
    {
        var sequence = 0;
        builder.OpenComponent<TComponent>(sequence++);

        foreach (var (name, value) in parameters)
            builder.AddAttribute(sequence++, name, value);

        builder.CloseComponent();
    };
}

