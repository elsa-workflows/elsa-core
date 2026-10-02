using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Enums;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Humanizer;
using Microsoft.AspNetCore.Components;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.LogPersistenceStrategies;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Api.Client.Serialization;
using Elsa.Api.Client.Shared.Enums;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs;

#pragma warning disable CS0612 // LegacyPersistenceActivityConfiguration is intentionally read for migration support.

/// <summary>
/// Represents the persistence tab.
/// </summary>
public partial class LogPersistenceTab
{
    /// An event raised when the activity is updated.
    [Parameter] public Func<JsonObject, Task>? OnActivityUpdated { get; set; }

    /// <summary>
    /// Gets or sets the workflow definition.
    /// </summary>
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }

    /// <summary>
    /// Gets or sets the activity to edit.
    /// </summary>
    [Parameter] public JsonObject? Activity { get; set; }

    /// <summary>
    /// The workspace.
    /// </summary>
    [CascadingParameter] public IWorkspace? Workspace { get; set; }

    /// <summary>
    /// Gets or sets the activity descriptor.
    /// </summary>
    [Parameter] public ActivityDescriptor? ActivityDescriptor { get; set; }

    [Inject] private ILogPersistenceStrategyService LogPersistenceStrategyService { get; set; } = null!;

    private ICollection<InputDescriptor> InputDescriptors { get; set; } = new List<InputDescriptor>();
    private ICollection<OutputDescriptor> OutputDescriptors { get; set; } = new List<OutputDescriptor>();
    private bool IsReadOnly => Workspace?.IsReadOnly == true;
    private LegacyPersistenceActivityConfiguration _legacyPersistenceConfiguration = new();
    private PersistenceActivityConfiguration _persistenceConfiguration = new();
    private ICollection<LogPersistenceStrategyDescriptor> _logPersistenceStrategyDescriptors = new List<LogPersistenceStrategyDescriptor>();
    private const string LogPersistenceModeKey = "logPersistenceMode";
    private const string LogPersistenceConfigKey = "logPersistenceConfig";

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _legacyPersistenceConfiguration = new();
        _persistenceConfiguration = new();
        _logPersistenceStrategyDescriptors = (await LogPersistenceStrategyService.GetLogPersistenceStrategiesAsync()).ToList();
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Activity == null || ActivityDescriptor == null)
            return;

        InputDescriptors = ActivityDescriptor.Inputs.ToList();
        OutputDescriptors = ActivityDescriptor.Outputs.ToList();

        SetPersistenceProperties();
    }

    private async Task OnDefaultStrategyChanged(string? value)
    {
        _persistenceConfiguration.Default = new()
        {
            EvaluationMode = LogPersistenceEvaluationMode.Strategy,
            StrategyType = value
        };
        await PersistLogPersistenceStrategies();
    }
    
    private async Task OnDefaultExpressionChanged(Expression? expression)
    {
        _persistenceConfiguration.Default = new()
        {
            EvaluationMode = LogPersistenceEvaluationMode.Expression,
            Expression = expression
        };
        await PersistLogPersistenceStrategies();
    }
    
    private async Task OnInternalStateStrategyChanged(string? value)
    {
        _persistenceConfiguration.InternalState = new()
        {
            EvaluationMode = LogPersistenceEvaluationMode.Strategy,
            StrategyType = value
        };
        await PersistLogPersistenceStrategies();
    }
    
    private async Task OnInternalStateExpressionChanged(Expression? expression)
    {
        _persistenceConfiguration.InternalState = new()
        {
            EvaluationMode = LogPersistenceEvaluationMode.Expression,
            Expression = expression
        };
        await PersistLogPersistenceStrategies();
    }

    private async Task OnPropertyStrategyChanged(string propertyName, IDictionary<string, LogPersistenceConfiguration?> properties, string? value)
    {
        SetLogPersistenceStrategyTypeName(propertyName, properties, value);
        await PersistLogPersistenceStrategies();
    }

    private async Task OnPropertyExpressionChanged(string propertyName, IDictionary<string, LogPersistenceConfiguration?> properties, Expression expression)
    {
        SetLogPersistenceExpression(propertyName, properties, expression);
        await PersistLogPersistenceStrategies();
    }

    private async Task PersistLogPersistenceStrategies()
    {
        var props = _persistenceConfiguration.SerializeToNode(SerializerOptions.LogPersistenceConfigSerializerOptions);
        Activity?.SetProperty(props, "customProperties", LogPersistenceConfigKey);

        await RaiseActivityUpdated();
    }

    private void SetPersistenceProperties()
    {
        var customProperties = GetCustomProperties();
        var legacyPersistence = ((JsonObject)customProperties).GetProperty<LegacyPersistenceActivityConfiguration>(SerializerOptions.LogPersistenceConfigSerializerOptions, LogPersistenceModeKey) ?? new LegacyPersistenceActivityConfiguration();
        var persistence = ((JsonObject)customProperties).GetProperty<PersistenceActivityConfiguration>(SerializerOptions.LogPersistenceConfigSerializerOptions, LogPersistenceConfigKey) ?? new PersistenceActivityConfiguration();

        _legacyPersistenceConfiguration = legacyPersistence;
        _persistenceConfiguration = persistence;
        UpgradeLegacyModel(_persistenceConfiguration, _legacyPersistenceConfiguration);

        if (Activity == null)
            return;

        var serializedLegacyPersistenceConfiguration = _legacyPersistenceConfiguration.SerializeToNode(SerializerOptions.LogPersistenceConfigSerializerOptions);
        var serializedPersistenceConfiguration = _persistenceConfiguration.SerializeToNode(SerializerOptions.LogPersistenceConfigSerializerOptions);
        Activity.SetProperty(serializedLegacyPersistenceConfiguration, "customProperties", LogPersistenceModeKey);
        Activity.SetProperty(serializedPersistenceConfiguration, "customProperties", LogPersistenceConfigKey);
    }

    private void UpgradeLegacyModel(PersistenceActivityConfiguration newModel, LegacyPersistenceActivityConfiguration oldModel)
    {
        // Update the new model with the legacy model.
        foreach (var input in oldModel.Inputs)
        {
            if (newModel.Inputs.ContainsKey(input.Key))
                continue;
            newModel.Inputs[input.Key] = new()
            {
                EvaluationMode = LogPersistenceEvaluationMode.Strategy,
                StrategyType = input.Value.ToString()
            };
        }

        foreach (var output in oldModel.Outputs)
        {
            if (newModel.Outputs.ContainsKey(output.Key))
                continue;
            newModel.Outputs[output.Key] = new()
            {
                EvaluationMode = LogPersistenceEvaluationMode.Strategy,
                StrategyType = output.Value.ToString()
            };
        }
    }

    private JsonNode GetCustomProperties()
    {
        var customProperties = Activity?.GetProperty("customProperties");
        if (customProperties != null) return customProperties;
        customProperties = new JsonObject(new List<KeyValuePair<string, JsonNode?>>());
        Activity?.SetProperty(customProperties, "customProperties");
        return customProperties;
    }

    private string? GetPropertyLogPersistenceStrategyTypeName(string propertyName, IDictionary<string, LogPersistenceConfiguration?> properties)
    {
        return GetLogPersistenceConfig(propertyName, properties)?.StrategyType;
    }
    
    private Expression? GetPropertyLogPersistenceExpression(string propertyName, IDictionary<string, LogPersistenceConfiguration?> properties)
    {
        return GetLogPersistenceConfig(propertyName, properties)?.Expression;
    }
    
    private LogPersistenceConfiguration? GetLogPersistenceConfig(string propertyName, IDictionary<string, LogPersistenceConfiguration?> properties)
    {
        var prop = propertyName.Camelize();
        return properties.TryGetValue(prop, out var v) ? v : null;
    }

    private void SetLogPersistenceStrategyTypeName(string propertyName, IDictionary<string, LogPersistenceConfiguration?> properties, string? value)
    {
        var currentConfig = GetLogPersistenceConfig(propertyName, properties);
        var prop = propertyName.Camelize();
        var config = new LogPersistenceConfiguration
        {
            EvaluationMode = LogPersistenceEvaluationMode.Strategy,
            StrategyType = value,
            Expression = currentConfig?.Expression
        };
        properties[prop] = config;
    }
    
    private void SetLogPersistenceExpression(string propertyName, IDictionary<string, LogPersistenceConfiguration?> properties, Expression expression)
    {
        var currentConfig = GetLogPersistenceConfig(propertyName, properties);
        var prop = propertyName.Camelize();
        var config = new LogPersistenceConfiguration
        {
            EvaluationMode = LogPersistenceEvaluationMode.Expression,
            Expression = expression,
            StrategyType = currentConfig?.StrategyType
        };
        properties[prop] = config;
    }

    [Obsolete]
    private LogPersistenceMode GetPropertyLogPersistenceMode(string propertyName, IDictionary<string, LogPersistenceMode> properties)
    {
        var prop = propertyName.Camelize();
        if (properties.All(o => o.Key != prop))
            properties[prop] = LogPersistenceMode.Inherit;
        return properties[prop];
    }

    [Obsolete]
    private void SetLogPersistenceMode(string propertyName, IDictionary<string, LogPersistenceMode> properties, LogPersistenceMode value)
    {
        var prop = propertyName.Camelize();
        properties[prop] = value;
    }

    private async Task RaiseActivityUpdated()
    {
        if (OnActivityUpdated != null)
            await OnActivityUpdated(Activity!);
    }

    private IDictionary<string, object> GetExpressionEditorProps(PropertyDescriptor? propertyDescriptor)
    {
        var props = new Dictionary<string, object>();
        
        if(ActivityDescriptor != null) props[nameof(ActivityDescriptor)] = ActivityDescriptor;
        if(propertyDescriptor != null) props[nameof(PropertyDescriptor)] = propertyDescriptor;
        props["WorkflowDefinitionId"] = WorkflowDefinition!.DefinitionId;
        
        return props;
    }
}

/// <summary>
/// Defines the Persistence Strategy Configuration for an activity
/// </summary>
[Obsolete]
/// <summary>
/// Represents the legacy persistence activity configuration.
/// </summary>
public class LegacyPersistenceActivityConfiguration
{
    /// <summary>
    /// Default Configuration Strategy for the activity
    /// </summary>
    public LogPersistenceMode Default { get; set; } = LogPersistenceMode.Inherit;

    /// <summary>
    /// Define Configuration Strategy for each Input properties
    /// </summary>
    public IDictionary<string, LogPersistenceMode> Inputs { get; set; } = new Dictionary<string, LogPersistenceMode>();

    /// <summary>
    /// Define Configuration Strategy for each Output properties
    /// </summary>
    public IDictionary<string, LogPersistenceMode> Outputs { get; set; } = new Dictionary<string, LogPersistenceMode>();
}

/// <summary>
/// Defines the Persistence Strategy Configuration for an activity
/// </summary>
public class PersistenceActivityConfiguration
{
    /// <summary>
    /// Default strategy type name for the entire activity.
    /// </summary>
    public LogPersistenceConfiguration? Default { get; set; }
    
    /// <summary>
    /// Strategy type name for internal activity state.
    /// </summary>
    public LogPersistenceConfiguration? InternalState { get; set; }

    /// <summary>
    /// Strategy type names for individual Input properties.
    /// </summary>
    public IDictionary<string, LogPersistenceConfiguration?> Inputs { get; set; } = new Dictionary<string, LogPersistenceConfiguration?>();

    /// <summary>
    /// Strategy type names for individual Output properties.
    /// </summary>
    public IDictionary<string, LogPersistenceConfiguration?> Outputs { get; set; } = new Dictionary<string, LogPersistenceConfiguration?>();
}
