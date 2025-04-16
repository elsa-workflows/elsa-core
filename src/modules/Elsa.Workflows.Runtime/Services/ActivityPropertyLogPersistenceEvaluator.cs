using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.LogPersistence;
using Elsa.Workflows.LogPersistence.Strategies;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Models;
using Elsa.Workflows.Serialization.Converters;
using Humanizer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime;

/* The following legacy JSON structure is expected to be found in the custom properties of the workflow and activity:
 * {
 *      "logPersistenceMode": {
 *          "default": "default",
 *          "inputs": { k : v },
 *          "outputs": { k: v }
 *          }
 *  }
 */

/* The following JSON structure is expected to be found in the custom properties of the workflow and activity:
 * {
 *      "logPersistenceConfig": {
 *          "default": { "evaluationMode": "Strategy", "strategyType": "Elsa.Workflows.LogPersistence.Strategies.Inherit, Elsa.Workflows.Core", "expression": "..." },
 *          "inputs": { "input1" : { "evaluationMode": "Strategy", "strategyType": "Elsa.Workflows.LogPersistence.Strategies.Inherit, Elsa.Workflows.Core", "expression": "..." } },
 *          "outputs": { "output1" : { "evaluationMode": "Strategy", "strategyType": "Elsa.Workflows.LogPersistence.Strategies.Inherit, Elsa.Workflows.Core", "expression": "..." } }
 *          }
 *  }
 */

public class ActivityPropertyLogPersistenceEvaluator : IActivityPropertyLogPersistenceEvaluator
{
    private readonly IExpressionEvaluator _expressionEvaluator;
    private readonly JsonSerializerOptions _logPersistenceConfigSerializerOptions;
    private readonly IDictionary<string, ILogPersistenceStrategy> _logPersistenceStrategies;
    private readonly IOptions<ManagementOptions> _options;
    private readonly ILogger _logger;

    public ActivityPropertyLogPersistenceEvaluator(
        ILogPersistenceStrategyService logPersistenceStrategyService,
        IExpressionDescriptorRegistry expressionDescriptorRegistry,
        IExpressionEvaluator expressionEvaluator,
        IOptions<ManagementOptions> options,
        ILogger<ActivityPropertyLogPersistenceEvaluator> logger
    )
    {
        _expressionEvaluator = expressionEvaluator;
        _options = options;
        _logger = logger;
        _logPersistenceStrategies = logPersistenceStrategyService.ListStrategies().ToDictionary(x => x.GetType().GetSimpleAssemblyQualifiedName(), x => x);
        _logPersistenceConfigSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }.WithConverters(
            new ExpressionJsonConverterFactory(expressionDescriptorRegistry),
            new JsonStringEnumConverter(),
            new ExpandoObjectConverterFactory());
    }

    private const string LegacyLogPersistenceModeKey = "logPersistenceMode";
    private const string LogPersistenceConfigKey = "logPersistenceConfig";

    public async Task<ActivityLogPersistenceModeMap> EvaluateLogPersistenceModesAsync(ActivityExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var legacyActivityPersistenceProperties = context.Activity.CustomProperties.GetValueOrDefault<IDictionary<string, object?>>(LegacyLogPersistenceModeKey, () => new Dictionary<string, object?>());
        var rootActivityExecutionContext = context.WorkflowExecutionContext.ActivityExecutionContexts.First(x => x.ParentActivityExecutionContext == null);
        var workflow = (Workflow?)context.GetAncestors().FirstOrDefault(x => x.Activity is Workflow)?.Activity ?? context.WorkflowExecutionContext.Workflow;
        var workflowPersistenceProperty = await GetDefaultPersistenceModeAsync(rootActivityExecutionContext.ExpressionExecutionContext, workflow.CustomProperties, () => _options.Value.LogPersistenceMode, cancellationToken);
        var activityPersistencePropertyDefault = await GetDefaultPersistenceModeAsync(context.ExpressionExecutionContext, context.Activity.CustomProperties, () => workflowPersistenceProperty, cancellationToken);
        var activityPersistenceProperties = context.Activity.CustomProperties.GetValueOrDefault<IDictionary<string, object?>>(LogPersistenceConfigKey, () => new Dictionary<string, object?>());
        var inputDescriptors = context.ActivityDescriptor.Inputs;
        var outputDescriptors = context.ActivityDescriptor.Outputs;
        var map = new ActivityLogPersistenceModeMap();

        await EvaluateLogPersistencePropertiesAsync(context, "inputs", inputDescriptors, legacyActivityPersistenceProperties, activityPersistenceProperties, activityPersistencePropertyDefault, map.Inputs, cancellationToken);
        await EvaluateLogPersistencePropertiesAsync(context, "outputs", outputDescriptors, legacyActivityPersistenceProperties, activityPersistenceProperties, activityPersistencePropertyDefault, map.Outputs, cancellationToken);
        
        return map;
    }

    private async Task EvaluateLogPersistencePropertiesAsync(
        ActivityExecutionContext context, 
        string key, 
        IEnumerable<PropertyDescriptor> propertyDescriptors, 
        IDictionary<string, object?>? obsoletePersistenceModeConfiguration,
        IDictionary<string, object?>? persistenceStrategyConfiguration,
        LogPersistenceMode defaultLogPersistenceMode,
        IDictionary<string, LogPersistenceMode> map,
        CancellationToken cancellationToken)
    {
        var legacyProps = obsoletePersistenceModeConfiguration!.GetValueOrDefault(key, () => new Dictionary<string, object?>());
        var props = persistenceStrategyConfiguration!.GetValueOrDefault(key, () => new Dictionary<string, object?>());
            
        foreach (var inputDescriptor in propertyDescriptors)
        {
            var logMode = await EvaluateLogPersistenceMode(
                context.ExpressionExecutionContext, 
                inputDescriptor, 
                legacyProps, 
                props, 
                defaultLogPersistenceMode, 
                cancellationToken);
            map[inputDescriptor.Name] = logMode;
        }
    }

    public async Task<LogPersistenceMode> EvaluateLogPersistenceModeAsync(ActivityExecutionContext context, PropertyDescriptor propertyDescriptor)
    {
        var cancellationToken = context.CancellationToken;
        var legacyActivityPersistenceProperties = context.Activity.CustomProperties.GetValueOrDefault<IDictionary<string, object?>>(LegacyLogPersistenceModeKey, () => new Dictionary<string, object?>());
        var rootActivityExecutionContext = context.WorkflowExecutionContext.ActivityExecutionContexts.First(x => x.ParentActivityExecutionContext == null);
        var workflow = (Workflow?)context.GetAncestors().FirstOrDefault(x => x.Activity is Workflow)?.Activity ?? context.WorkflowExecutionContext.Workflow;
        var workflowPersistenceProperty = await GetDefaultPersistenceModeAsync(rootActivityExecutionContext.ExpressionExecutionContext, workflow.CustomProperties, () => _options.Value.LogPersistenceMode, cancellationToken);
        var activityPersistencePropertyDefault = await GetDefaultPersistenceModeAsync(context.ExpressionExecutionContext, context.Activity.CustomProperties, () => workflowPersistenceProperty, cancellationToken);
        var activityPersistenceProperties = context.Activity.CustomProperties.GetValueOrDefault<IDictionary<string, object?>>(LogPersistenceConfigKey, () => new Dictionary<string, object?>());

        return await EvaluateLogPersistenceMode(
            context.ExpressionExecutionContext,
            propertyDescriptor,
            legacyActivityPersistenceProperties,
            activityPersistenceProperties,
            activityPersistencePropertyDefault,
            cancellationToken);
    }

    public async Task<LogPersistenceMode> EvaluateLogPersistenceMode(
        ExpressionExecutionContext expressionExecutionContext,
        PropertyDescriptor propertyDescriptor,
        IDictionary<string, object?>? obsoletePersistenceModeConfiguration,
        IDictionary<string, object?>? persistenceStrategyConfiguration,
        LogPersistenceMode defaultLogPersistenceMode,
        CancellationToken cancellationToken)
    {
        var propKey = propertyDescriptor.Name.Camelize();
        var logPersistenceConfigObject = persistenceStrategyConfiguration?.GetValueOrDefault(propKey, () => null);
        var logPersistenceConfig = Convert(logPersistenceConfigObject);
        var mode = logPersistenceConfig != null
            ? await EvaluateLogPersistenceConfigAsync(logPersistenceConfig, expressionExecutionContext, () => defaultLogPersistenceMode, cancellationToken)
            : obsoletePersistenceModeConfiguration!?.GetValueOrDefault(propKey, () => defaultLogPersistenceMode);

        return mode ?? defaultLogPersistenceMode;
    }

    public async Task<Dictionary<string, object?>> GetPersistableOutputAsync(ActivityExecutionContext context)
    {
        var cancellationToken = context.WorkflowExecutionContext.CancellationToken;
        var legacyActivityPersistenceProperties = context.Activity.CustomProperties.GetValueOrDefault<IDictionary<string, object?>>(LegacyLogPersistenceModeKey, () => new Dictionary<string, object?>());
        var rootActivityExecutionContext = context.WorkflowExecutionContext.ActivityExecutionContexts.First(x => x.ParentActivityExecutionContext == null);
        var workflow = (Workflow?)context.GetAncestors().FirstOrDefault(x => x.Activity is Workflow)?.Activity ?? context.WorkflowExecutionContext.Workflow;
        var workflowPersistenceProperty = await GetDefaultPersistenceModeAsync(rootActivityExecutionContext.ExpressionExecutionContext, workflow.CustomProperties, () => _options.Value.LogPersistenceMode, cancellationToken);
        var activityPersistencePropertyDefault = await GetDefaultPersistenceModeAsync(context.ExpressionExecutionContext, context.Activity.CustomProperties, () => workflowPersistenceProperty, cancellationToken);
        var activityPersistenceProperties = context.Activity.CustomProperties.GetValueOrDefault<IDictionary<string, object?>>(LogPersistenceConfigKey, () => new Dictionary<string, object?>());

        return await GetPersistableOutputAsync(
            context,
            legacyActivityPersistenceProperties,
            activityPersistenceProperties,
            activityPersistencePropertyDefault,
            cancellationToken);
    }

    public async Task<Dictionary<string, object?>> GetPersistableOutputAsync(
        ActivityExecutionContext context,
        IDictionary<string, object?> legacyActivityPersistenceProperties,
        IDictionary<string, object?> activityPersistenceProperties,
        LogPersistenceMode activityPersistencePropertyDefault,
        CancellationToken cancellationToken)
    {
        var outputs = context.GetOutputs();
        return await GetPersistablePropertiesAsync(context, outputs, "outputs", legacyActivityPersistenceProperties, activityPersistenceProperties, activityPersistencePropertyDefault, cancellationToken);
    }

    public async Task<Dictionary<string, object?>> GetPersistableInputAsync(ActivityExecutionContext context,
        IDictionary<string, object?> legacyActivityPersistenceProperties,
        IDictionary<string, object?> activityPersistenceProperties,
        LogPersistenceMode activityPersistencePropertyDefault,
        CancellationToken cancellationToken)
    {
        return await GetPersistablePropertiesAsync(
            context,
            context.ActivityState!,
            "inputs",
            legacyActivityPersistenceProperties,
            activityPersistenceProperties,
            activityPersistencePropertyDefault,
            cancellationToken);
    }

    public async Task<Dictionary<string, object?>> GetPersistablePropertiesAsync(
        ActivityExecutionContext context,
        IDictionary<string, object?> state,
        string key,
        IDictionary<string, object?> legacyActivityPersistenceProperties,
        IDictionary<string, object?> activityPersistenceProperties,
        LogPersistenceMode activityPersistencePropertyDefault,
        CancellationToken cancellationToken)
    {
        return await FilterPropertiesUsingPersistenceMode(
            context.ExpressionExecutionContext,
            state,
            legacyActivityPersistenceProperties!.GetValueOrDefault(key, () => new Dictionary<string, object>())!,
            activityPersistenceProperties!.GetValueOrDefault(key, () => new Dictionary<string, object>())!,
            activityPersistencePropertyDefault,
            cancellationToken);
    }

    public async Task<LogPersistenceMode> GetDefaultPersistenceModeAsync(ExpressionExecutionContext expressionExecutionContext, IDictionary<string, object> customProperties, Func<LogPersistenceMode> defaultFactory, CancellationToken cancellationToken)
    {
        var legacyProperties = customProperties.GetValueOrDefault<IDictionary<string, object?>>(LegacyLogPersistenceModeKey, () => new Dictionary<string, object?>());
        var properties = customProperties.GetValueOrDefault<IDictionary<string, object>>(LogPersistenceConfigKey, () => new Dictionary<string, object>());
        var defaultPersistenceConfigObject = properties?.TryGetValue("default", out var defaultPersistenceConfigObjectValue) == true ? defaultPersistenceConfigObjectValue : null;

        if (defaultPersistenceConfigObject == null)
        {
            var legacyPersistencePropertyDefault = legacyProperties!.GetValueOrDefault("default", defaultFactory);

            if (legacyPersistencePropertyDefault == LogPersistenceMode.Inherit)
                return defaultFactory();
            return legacyPersistencePropertyDefault;
        }

        var defaultPersistenceConfig = Convert(defaultPersistenceConfigObject);
        return await EvaluateLogPersistenceConfigAsync(defaultPersistenceConfig, expressionExecutionContext, defaultFactory, cancellationToken);
    }

    public async Task<Dictionary<string, object?>> FilterPropertiesUsingPersistenceMode(
        ExpressionExecutionContext expressionExecutionContext,
        IDictionary<string, object?> state,
        IDictionary<string, object> obsoletePersistenceModeConfiguration,
        IDictionary<string, object> persistenceStrategyConfiguration,
        LogPersistenceMode defaultLogPersistenceMode,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, object?>();

        foreach (var value in state)
        {
            var propKey = value.Key.Camelize();
            var logPersistenceConfigObject = persistenceStrategyConfiguration.GetValueOrDefault(propKey, () => null);
            var logPersistenceConfig = Convert(logPersistenceConfigObject);
            var mode = logPersistenceConfig != null
                ? await EvaluateLogPersistenceConfigAsync(logPersistenceConfig, expressionExecutionContext, () => defaultLogPersistenceMode, cancellationToken)
                : obsoletePersistenceModeConfiguration.GetValueOrDefault(propKey, () => defaultLogPersistenceMode);

            if (mode == LogPersistenceMode.Include || mode == LogPersistenceMode.Inherit && defaultLogPersistenceMode is LogPersistenceMode.Include or LogPersistenceMode.Inherit)
                result.Add(value.Key, value.Value);
        }

        return result;
    }

    public async Task<LogPersistenceMode> EvaluateLogPersistenceConfigAsync(LogPersistenceConfiguration? config, ExpressionExecutionContext executionContext, Func<LogPersistenceMode> defaultMode, CancellationToken cancellationToken)
    {
        if (config == null)
            return defaultMode();

        if (config.EvaluationMode == LogPersistenceEvaluationMode.Strategy)
        {
            var strategyTypeName = config.StrategyType ?? typeof(Inherit).GetSimpleAssemblyQualifiedName();
            var strategy = _logPersistenceStrategies.TryGetValue(strategyTypeName, out var v) ? v : null;

            if (strategy == null)
                return defaultMode();

            var strategyContext = new LogPersistenceStrategyContext(cancellationToken);
            var logMode = await strategy.GetPersistenceModeAsync(strategyContext);
            return logMode == LogPersistenceMode.Inherit ? defaultMode() : logMode;
        }

        if (config.Expression == null)
            return defaultMode();

        var expression = config.Expression;

        try
        {
            return await _expressionEvaluator.EvaluateAsync<LogPersistenceMode>(expression, executionContext);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Error evaluating log persistence expression");
            return defaultMode();
        }
    }

    private LogPersistenceConfiguration? Convert(object? value)
    {
        if (value == null)
            return null;

        if (value is LogPersistenceConfiguration c)
            return c;

        var json = JsonSerializer.Serialize(value);
        var config = JsonSerializer.Deserialize<LogPersistenceConfiguration>(json, _logPersistenceConfigSerializerOptions);

        return config;
    }
}

public class ActivityLogPersistenceModeMap
{
    public IDictionary<string, LogPersistenceMode> Inputs { get; set; } = new Dictionary<string, LogPersistenceMode>();
    public IDictionary<string, LogPersistenceMode> Outputs { get; set; } = new Dictionary<string, LogPersistenceMode>();
}