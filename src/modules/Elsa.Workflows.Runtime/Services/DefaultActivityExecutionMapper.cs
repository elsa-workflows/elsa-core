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
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Serialization.Converters;
using Elsa.Workflows.State;
using Humanizer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class DefaultActivityExecutionMapper : IActivityExecutionMapper
{
    private readonly JsonSerializerOptions _logPersistenceConfigSerializerOptions;
    private readonly IOptions<ManagementOptions> _options;
    private readonly IExpressionEvaluator _expressionEvaluator;
    private readonly ILogger<DefaultActivityExecutionMapper> _logger;
    private readonly IDictionary<string, ILogPersistenceStrategy> _logPersistenceStrategies;

    public DefaultActivityExecutionMapper(
        IOptions<ManagementOptions> options,
        ILogPersistenceStrategyService logPersistenceStrategyService,
        IExpressionEvaluator expressionEvaluator,
        IExpressionDescriptorRegistry expressionDescriptorRegistry,
        ILogger<DefaultActivityExecutionMapper> logger)
    {
        _options = options;
        _expressionEvaluator = expressionEvaluator;
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

    /// <inheritdoc />
    public async Task<ActivityExecutionRecord> MapAsync(ActivityExecutionContext source)
    {
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

        var cancellationToken = source.WorkflowExecutionContext.CancellationToken;
        var workflow = (Workflow?)source.GetAncestors().FirstOrDefault(x => x.Activity is Workflow)?.Activity ?? source.WorkflowExecutionContext.Workflow;
        var rootActivityExecutionContext = source.WorkflowExecutionContext.ActivityExecutionContexts.First(x => x.ParentActivityExecutionContext == null);
        var workflowPersistenceProperty = await GetDefaultPersistenceModeAsync(rootActivityExecutionContext.ExpressionExecutionContext, workflow.CustomProperties, () => _options.Value.LogPersistenceMode, cancellationToken);
        var activityPersistencePropertyDefault = await GetDefaultPersistenceModeAsync(source.ExpressionExecutionContext, source.Activity.CustomProperties, () => workflowPersistenceProperty, cancellationToken);
        var legacyActivityPersistenceProperties = source.Activity.CustomProperties.GetValueOrDefault<IDictionary<string, object?>>(LegacyLogPersistenceModeKey, () => new Dictionary<string, object?>());
        var activityPersistenceProperties = source.Activity.CustomProperties.GetValueOrDefault<IDictionary<string, object?>>(LogPersistenceConfigKey, () => new Dictionary<string, object?>());
        var payload = GetPayload(source);
        var outputs = GetOutputs(source);

        outputs = await StorePropertyUsingPersistenceMode(
            source.ExpressionExecutionContext,
            outputs,
            legacyActivityPersistenceProperties!.GetValueOrDefault("outputs", () => new Dictionary<string, object>())!,
            activityPersistenceProperties!.GetValueOrDefault("outputs", () => new Dictionary<string, object>())!,
            activityPersistencePropertyDefault,
            cancellationToken);

        var inputs = await StorePropertyUsingPersistenceMode(
            source.ExpressionExecutionContext,
            source.ActivityState,
            legacyActivityPersistenceProperties!.GetValueOrDefault("inputs", () => new Dictionary<string, object>())!,
            activityPersistenceProperties!.GetValueOrDefault("inputs", () => new Dictionary<string, object>())!,
            activityPersistencePropertyDefault,
            cancellationToken);

        return new ActivityExecutionRecord
        {
            Id = source.Id,
            ActivityId = source.Activity.Id,
            ActivityNodeId = source.NodeId,
            WorkflowInstanceId = source.WorkflowExecutionContext.Id,
            ActivityType = source.Activity.Type,
            ActivityName = source.Activity.Name,
            ActivityState = inputs,
            Outputs = outputs,
            Properties = source.Properties,
            Payload = payload,
            Exception = ExceptionState.FromException(source.Exception),
            ActivityTypeVersion = source.Activity.Version,
            StartedAt = source.StartedAt,
            HasBookmarks = source.Bookmarks.Any(),
            Status = source.Status,
            CompletedAt = source.CompletedAt
        };
    }

    private async Task<LogPersistenceMode> GetDefaultPersistenceModeAsync(ExpressionExecutionContext expressionExecutionContext, IDictionary<string, object> customProperties, Func<LogPersistenceMode> defaultFactory, CancellationToken cancellationToken)
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

    private async Task<Dictionary<string, object?>> StorePropertyUsingPersistenceMode(
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
            var mode = defaultLogPersistenceMode;

            if (logPersistenceConfig != null)
            {
                mode = await EvaluateLogPersistenceConfigAsync(logPersistenceConfig, expressionExecutionContext, () => defaultLogPersistenceMode, cancellationToken);
            }
            else
            {
                mode = obsoletePersistenceModeConfiguration.GetValueOrDefault(propKey, () => defaultLogPersistenceMode);
            }

            if (mode == LogPersistenceMode.Include || mode == LogPersistenceMode.Inherit && defaultLogPersistenceMode is LogPersistenceMode.Include or LogPersistenceMode.Inherit)
                result.Add(value.Key, value.Value);
        }

        return result;
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

    private async Task<LogPersistenceMode> EvaluateLogPersistenceConfigAsync(LogPersistenceConfiguration? config, ExpressionExecutionContext executionContext, Func<LogPersistenceMode> defaultMode, CancellationToken cancellationToken)
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

    private static IDictionary<string, object> GetPayload(ActivityExecutionContext source)
    {
        var outcomes = source.JournalData.TryGetValue("Outcomes", out var resultValue) ? resultValue as string[] : null;
        var payload = new Dictionary<string, object>();

        if (outcomes != null)
            payload.Add("Outcomes", outcomes);

        return payload;
    }

    private static IDictionary<string, object?> GetOutputs(ActivityExecutionContext source)
    {
        var activity = source.Activity;
        var expressionExecutionContext = source.ExpressionExecutionContext;
        var activityDescriptor = source.ActivityDescriptor;
        var outputDescriptors = activityDescriptor.Outputs;

        var outputs = outputDescriptors.ToDictionary(x => x.Name, x =>
        {
            if (x.IsSerializable == false)
                return "(not serializable)";

            var cachedValue = activity.GetOutput(expressionExecutionContext, x.Name);

            if (cachedValue != null)
                return cachedValue;

            if (x.ValueGetter(activity) is Output output && source.TryGet(output.MemoryBlockReference(), out var outputValue))
                return outputValue;

            return null;
        });

        return outputs;
    }
}