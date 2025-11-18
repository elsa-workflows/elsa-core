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
 *          "internalState": { "evaluationMode": "Strategy", "strategyType": "Elsa.Workflows.LogPersistence.Strategies.Inherit, Elsa.Workflows.Core", "expression": "..." }
 *          }
 *  }
 */
public class ActivityPropertyLogPersistenceEvaluator : IActivityPropertyLogPersistenceEvaluator
{
    private readonly IExpressionEvaluator _expressionEvaluator;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IDictionary<string, ILogPersistenceStrategy> _strategies;
    private readonly IOptions<ManagementOptions> _options;
    private readonly ILogger _logger;

    private const string LegacyKey = "logPersistenceMode";
    private const string ConfigKey = "logPersistenceConfig";
    private const string DefaultPersistenceKey = "default";
    private const string InternalStatePersistenceKey = "internalState";

    public ActivityPropertyLogPersistenceEvaluator(
        ILogPersistenceStrategyService strategyService,
        IExpressionDescriptorRegistry expressionDescriptorRegistry,
        IExpressionEvaluator expressionEvaluator,
        IOptions<ManagementOptions> options,
        ILogger<ActivityPropertyLogPersistenceEvaluator> logger)
    {
        _expressionEvaluator = expressionEvaluator;
        _options = options;
        _logger = logger;
        _strategies = strategyService.ListStrategies().ToDictionary(x => x.GetType().GetSimpleAssemblyQualifiedName(), x => x);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }.WithConverters(
            new ExpressionJsonConverterFactory(expressionDescriptorRegistry),
            new JsonStringEnumConverter(),
            new ExpandoObjectConverterFactory());
    }

    public async Task<ActivityLogPersistenceModeMap> EvaluateLogPersistenceModesAsync(ActivityExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var (legacyProps, configProps, defaultMode, internalStateMode) = await GetPersistenceDefaultsAsync(context, cancellationToken);
        var map = new ActivityLogPersistenceModeMap();

        await EvaluatePropertiesAsync(context, "inputs", context.ActivityDescriptor.Inputs, legacyProps, configProps, defaultMode, map.Inputs, cancellationToken);
        await EvaluatePropertiesAsync(context, "outputs", context.ActivityDescriptor.Outputs, legacyProps, configProps, defaultMode, map.Outputs, cancellationToken);
        map.InternalState = internalStateMode;

        return map;
    }

    public async Task<Dictionary<string, object>> GetPersistableOutputAsync(ActivityExecutionContext context)
    {
        var cancellationToken = context.WorkflowExecutionContext.CancellationToken;
        var (legacyProps, configProps, defaultMode, _) = await GetPersistenceDefaultsAsync(context, cancellationToken);
        var outputs = context.GetOutputs();
        return await GetPersistablePropertiesAsync(context, outputs, "outputs", legacyProps, configProps, defaultMode, cancellationToken);
    }

    private async Task<(IDictionary<string, object> legacyProps, IDictionary<string, object> configProps, LogPersistenceMode defaultMode, LogPersistenceMode internalStateDefaultMode)> GetPersistenceDefaultsAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
    {
        var legacyProps = context.Activity.CustomProperties.GetValueOrDefault<IDictionary<string, object>>(LegacyKey, () => new Dictionary<string, object>())!;
        var configProps = context.Activity.CustomProperties.GetValueOrDefault<IDictionary<string, object>>(ConfigKey, () => new Dictionary<string, object>())!;
        var rootContext = context.WorkflowExecutionContext.ActivityExecutionContexts.First(x => x.ParentActivityExecutionContext == null);
        var workflow = (Workflow?)context.GetAncestors().FirstOrDefault(x => x.Activity is Workflow)?.Activity ?? context.WorkflowExecutionContext.Workflow;

        // Resolve defaultMode used for inputs and outputs. Resolution hierarchy: activity default -> workflow default -> system default from options.
        var workflowDefaultMode = await GetPersistenceModeAsync(rootContext.ExpressionExecutionContext, workflow.CustomProperties, DefaultPersistenceKey, () => _options.Value.LogPersistenceMode, cancellationToken);
        var defaultMode = await GetPersistenceModeAsync(context.ExpressionExecutionContext, context.Activity.CustomProperties, DefaultPersistenceKey, () => workflowDefaultMode, cancellationToken);

        // Resolve internalStateMode for internal state. Resolution hierarchy: activity internal state -> activity default -> workflow internal state -> workflow default -> system default from options.
        var workflowInternalStateDefaultMode = await GetPersistenceModeAsync(rootContext.ExpressionExecutionContext, workflow.CustomProperties, InternalStatePersistenceKey, () => workflowDefaultMode, cancellationToken);
        var activityDefaultMode = await GetPersistenceModeAsync(context.ExpressionExecutionContext, context.Activity.CustomProperties, DefaultPersistenceKey, () => workflowInternalStateDefaultMode, cancellationToken);
        var internalStateMode = await GetPersistenceModeAsync(context.ExpressionExecutionContext, context.Activity.CustomProperties, InternalStatePersistenceKey, () => activityDefaultMode, cancellationToken);

        return (legacyProps, configProps, defaultMode, internalStateMode);
    }

    private async Task EvaluatePropertiesAsync(
        ActivityExecutionContext context,
        string key,
        IEnumerable<PropertyDescriptor> descriptors,
        IDictionary<string, object> legacyConfig,
        IDictionary<string, object> currentConfig,
        LogPersistenceMode defaultMode,
        IDictionary<string, LogPersistenceMode> resultMap,
        CancellationToken cancellationToken)
    {
        var legacySection = legacyConfig.GetValueOrDefault(key, () => new Dictionary<string, object>())!;
        var currentSection = currentConfig.GetValueOrDefault(key, () => new Dictionary<string, object>())!;

        foreach (var descriptor in descriptors)
        {
            resultMap[descriptor.Name] = await EvaluatePropertyModeAsync(context.ExpressionExecutionContext, descriptor, legacySection, currentSection, defaultMode, cancellationToken);
        }
    }

    private async Task<LogPersistenceMode> EvaluatePropertyModeAsync(
        ExpressionExecutionContext executionContext,
        PropertyDescriptor descriptor,
        IDictionary<string, object> legacySection,
        IDictionary<string, object> currentSection,
        LogPersistenceMode defaultMode,
        CancellationToken cancellationToken)
    {
        var key = descriptor.Name.Camelize();
        var configObject = currentSection.GetValueOrDefault(key, () => null);
        var config = ConvertToConfig(configObject);
        if (config != null)
            return await EvaluateConfigAsync(config, executionContext, () => defaultMode, cancellationToken);

        var mode = legacySection.GetValueOrDefault(key, () => defaultMode);
        return ResolveMode(mode, () => defaultMode);
    }

    private async Task<Dictionary<string, object>> GetPersistablePropertiesAsync(
        ActivityExecutionContext context,
        IDictionary<string, object> state,
        string key,
        IDictionary<string, object> legacyConfig,
        IDictionary<string, object> currentConfig,
        LogPersistenceMode defaultMode,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, object>();
        var legacySection = legacyConfig.GetValueOrDefault(key, () => new Dictionary<string, object>());
        var currentSection = currentConfig.GetValueOrDefault(key, () => new Dictionary<string, object>());

        foreach (var item in state)
        {
            var propKey = item.Key.Camelize();
            var configObject = currentSection!.GetValueOrDefault(propKey, () => null);
            var config = ConvertToConfig(configObject);
            var mode = config != null
                ? await EvaluateConfigAsync(config, context.ExpressionExecutionContext, () => defaultMode, cancellationToken)
                : legacySection!.GetValueOrDefault(propKey, () => defaultMode);

            if (mode == LogPersistenceMode.Include || (mode == LogPersistenceMode.Inherit && (defaultMode == LogPersistenceMode.Include || defaultMode == LogPersistenceMode.Inherit)))
                result.Add(item.Key, item.Value);
        }

        return result;
    }

    private async Task<LogPersistenceMode> GetPersistenceModeAsync(
        ExpressionExecutionContext executionContext,
        IDictionary<string, object> properties,
        string persistenceKey,
        Func<LogPersistenceMode> defaultFactory,
        CancellationToken cancellationToken)
    {
        var legacyProps = properties.GetValueOrDefault<IDictionary<string, object>>(LegacyKey, () => new Dictionary<string, object>());
        var configProps = properties.GetValueOrDefault<IDictionary<string, object>>(ConfigKey, () => new Dictionary<string, object>());
        var defaultObj = configProps!.TryGetValue(persistenceKey, out var val) ? val : null;
        if (defaultObj == null)
        {
            var legacyDefault = legacyProps!.GetValueOrDefault(persistenceKey, defaultFactory);
            return legacyDefault == LogPersistenceMode.Inherit ? defaultFactory() : legacyDefault;
        }

        var config = ConvertToConfig(defaultObj);
        return await EvaluateConfigAsync(config, executionContext, defaultFactory, cancellationToken);
    }

    private async Task<LogPersistenceMode> EvaluateConfigAsync(
        LogPersistenceConfiguration? config,
        ExpressionExecutionContext executionContext,
        Func<LogPersistenceMode> defaultFactory,
        CancellationToken cancellationToken)
    {
        if (config?.EvaluationMode == LogPersistenceEvaluationMode.Strategy)
        {
            var strategyType = config.StrategyType ?? typeof(Inherit).GetSimpleAssemblyQualifiedName();
            if (!_strategies.TryGetValue(strategyType, out var strategy))
                return defaultFactory();

            var strategyContext = new LogPersistenceStrategyContext(cancellationToken);
            var mode = await strategy.GetPersistenceModeAsync(strategyContext);
            return ResolveMode(mode, defaultFactory);
        }

        if (config?.Expression == null)
            return defaultFactory();

        try
        {
            var mode = await _expressionEvaluator.EvaluateAsync<LogPersistenceMode>(config.Expression, executionContext);
            return ResolveMode(mode, defaultFactory);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error evaluating log persistence expression");
            return defaultFactory();
        }
    }

    private LogPersistenceMode ResolveMode(LogPersistenceMode mode, Func<LogPersistenceMode> defaultFactory)
    {
        var m = mode == LogPersistenceMode.Inherit ? defaultFactory() : mode;
        return m == LogPersistenceMode.Inherit ? LogPersistenceMode.Include : m;
    }

    private LogPersistenceConfiguration? ConvertToConfig(object? value)
    {
        if (value == null) return null;
        if (value is LogPersistenceConfiguration config) return config;
        var json = JsonSerializer.Serialize(value);
        return JsonSerializer.Deserialize<LogPersistenceConfiguration>(json, _jsonOptions);
    }
}