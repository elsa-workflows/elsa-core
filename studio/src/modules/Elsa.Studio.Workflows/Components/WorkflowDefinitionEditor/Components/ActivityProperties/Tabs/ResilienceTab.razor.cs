using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.Resilience.Models;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Components;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs;

/// <summary>
/// The Task tab for an activity.
/// </summary>
public partial class ResilienceTab
{
    private const string DefaultCategory = "Default";
    private bool _isInitialized;

    /// <summary>
    /// Gets or sets the workflow definition.
    /// </summary>
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }

    /// The activity.
    [Parameter] public JsonObject? Activity { get; set; }

    /// The activity descriptor.
    [Parameter] public ActivityDescriptor? ActivityDescriptor { get; set; } = null!;

    /// An event raised when the activity is updated.
    [Parameter] public Func<JsonObject, Task>? OnActivityUpdated { get; set; }

    [Inject] private IResilienceStrategyCatalog ResilienceStrategyCatalog { get; set; } = null!;
    [CascadingParameter] private IWorkspace? Workspace { get; set; }
    private ExpressionEditor? ExpressionEditor { get; set; }
    private bool IsReadOnly => Workspace?.IsReadOnly == true;
    private ICollection<JsonObject> ResilienceStrategies { get; set; } = [];
    private string ResilienceCategory { get; set; } = DefaultCategory;
    private ResilienceStrategyConfig? ResilienceStrategyConfig { get; set; }
    private Expression? Expression { get; set; }
    private string? ResilienceStrategyId { get; set; }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (Activity == null || ActivityDescriptor == null)
            return;
        
        await SetPropertiesAsync();

        if (!_isInitialized)
        {
            if (ResilienceStrategyConfig?.Mode == ResilienceStrategyConfigMode.Expression && Expression != null && ExpressionEditor != null)
            {
                _isInitialized = true;
                await ExpressionEditor.UpdateAsync(Expression);
            }
        }
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var strategies = await ResilienceStrategyCatalog.ListAsync(ResilienceCategory);
        ResilienceStrategies = strategies.ToList();
    }

    private IDictionary<string, object> GetExpressionEditorProps()
    {
        var props = new Dictionary<string, object>();

        if (ActivityDescriptor != null) props[nameof(ActivityDescriptor)] = ActivityDescriptor;
        props["WorkflowDefinitionId"] = WorkflowDefinition!.DefinitionId;

        return props;
    }

    private async Task RaiseActivityUpdatedAsync()
    {
        if (OnActivityUpdated != null)
            await OnActivityUpdated(Activity!);
    }

    private async Task PersistStrategyConfigAsync()
    {
        var config = ResilienceStrategyConfig ?? new ResilienceStrategyConfig();
        var expressionType = Expression?.Type ?? "Default";
        config.Expression = Expression;
        config.StrategyId = ResilienceStrategyId;
        config.Mode = expressionType == "Default" ? ResilienceStrategyConfigMode.Identifier : ResilienceStrategyConfigMode.Expression;
        ResilienceStrategyConfig = config;
        Activity?.SetResilienceStrategy(config);

        await RaiseActivityUpdatedAsync();
    }

    private async Task SetPropertiesAsync()
    {
        if (Activity == null || ActivityDescriptor == null)
            return;

        var config = Activity.GetResilienceStrategy();
        var previousCategory = ResilienceCategory;
        ResilienceCategory = ActivityDescriptor.CustomProperties.TryGetValue("ResilienceCategory", out var category) ? category.ToString() ?? DefaultCategory : DefaultCategory;
        ResilienceStrategyConfig = config;
        Expression = config?.Expression;
        ResilienceStrategyId = config?.StrategyId;
        
        if (previousCategory != ResilienceCategory || ResilienceStrategies.Count == 0)
        {
            var strategies = await ResilienceStrategyCatalog.ListAsync(ResilienceCategory);
            ResilienceStrategies = strategies.ToList();
        }
    }

    private async Task OnExpressionChangedAsync(Expression? expression)
    {
        Expression = expression;
        await PersistStrategyConfigAsync();
    }

    private async Task OnStrategyChanged(string id)
    {
        ResilienceStrategyId = id;
        await PersistStrategyConfigAsync();
    }
}