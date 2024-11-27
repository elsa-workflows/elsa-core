using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.LogPersistence;
using Elsa.Workflows.LogPersistence.Strategies;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.State;
using Humanizer;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class DefaultActivityExecutionMapper : IActivityExecutionMapper
{
    private readonly IOptions<ManagementOptions> _options;
    private readonly IExpressionEvaluator _expressionEvaluator;
    private readonly IDictionary<string, ILogPersistenceStrategy> _logPersistenceStrategies;

    public DefaultActivityExecutionMapper(IOptions<ManagementOptions> options, ILogPersistenceStrategyService logPersistenceStrategyService, IExpressionEvaluator expressionEvaluator)
    {
        _options = options;
        _expressionEvaluator = expressionEvaluator;
        _logPersistenceStrategies = logPersistenceStrategyService.ListStrategies().ToDictionary(x => x.GetType().GetSimpleAssemblyQualifiedName(), x => x);
    }

    private const string LegacyLogPersistenceModeKey = "logPersistenceMode";
    private const string LogPersistenceStrategyKey = "logPersistenceStrategy";

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
         *      "logPersistenceStrategy": {
         *          "default": "null",
         *          "inputs": { k : v },
         *          "outputs": { k: v }
         *          }
         *  }
         */

        var cancellationToken = source.WorkflowExecutionContext.CancellationToken;
        var workflow = (Workflow?)source.GetAncestors().FirstOrDefault(x => x.Activity is Workflow)?.Activity ?? source.WorkflowExecutionContext.Workflow;
        var workflowPersistenceProperty = await GetDefaultPersistenceModeAsync(workflow.CustomProperties, () => _options.Value.LogPersistenceMode, cancellationToken);
        var activityPersistencePropertyDefault = await GetDefaultPersistenceModeAsync(source.Activity.CustomProperties, () => workflowPersistenceProperty, cancellationToken);
        var legacyActivityPersistenceProperties = source.Activity.CustomProperties.GetValueOrDefault<IDictionary<string, object?>>(LegacyLogPersistenceModeKey, () => new Dictionary<string, object?>());
        var activityPersistenceProperties = source.Activity.CustomProperties.GetValueOrDefault<IDictionary<string, object?>>(LogPersistenceStrategyKey, () => new Dictionary<string, object?>());
        var payload = GetPayload(source);
        var outputs = GetOutputs(source);

        outputs = await StorePropertyUsingPersistenceMode(
            source,
            outputs,
            legacyActivityPersistenceProperties!.GetValueOrDefault("outputs", () => new Dictionary<string, object>())!,
            activityPersistenceProperties!.GetValueOrDefault("outputs", () => new Dictionary<string, LogPersistenceConfiguration>())!,
            activityPersistencePropertyDefault,
            cancellationToken);

        var inputs = await StorePropertyUsingPersistenceMode(
            source, 
            source.ActivityState,
            legacyActivityPersistenceProperties!.GetValueOrDefault("inputs", () => new Dictionary<string, object>())!,
            activityPersistenceProperties!.GetValueOrDefault("inputs", () => new Dictionary<string, LogPersistenceConfiguration>())!,
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
            Status = GetAggregateStatus(source),
            CompletedAt = source.CompletedAt
        };
    }

    private async Task<LogPersistenceMode> GetDefaultPersistenceModeAsync(IDictionary<string, object> customProperties, Func<LogPersistenceMode> defaultFactory, CancellationToken cancellationToken)
    {
        var legacyProperties = customProperties.GetValueOrDefault<IDictionary<string, object?>>(LegacyLogPersistenceModeKey, () => new Dictionary<string, object?>());
        var properties = customProperties.GetValueOrDefault<IDictionary<string, object?>>(LogPersistenceStrategyKey, () => new Dictionary<string, object?>());
        var defaultPersistenceStrategyTypeName = properties!.GetValueOrDefault<string>("default");

        if (defaultPersistenceStrategyTypeName == null)
        {
            var legacyPersistencePropertyDefault = legacyProperties!.GetValueOrDefault("default", defaultFactory);

            if (legacyPersistencePropertyDefault == LogPersistenceMode.Inherit)
                return defaultFactory();
            return legacyPersistencePropertyDefault;
        }

        var strategy = _logPersistenceStrategies.TryGetValue(defaultPersistenceStrategyTypeName, out var v) ? v : null;

        if (strategy == null)
            return defaultFactory();

        var strategyContext = new LogPersistenceStrategyContext(cancellationToken);
        return await strategy.GetPersistenceModeAsync(strategyContext);
    }

    private async Task<Dictionary<string, object?>> StorePropertyUsingPersistenceMode(
        ActivityExecutionContext activityExecutionContext,
        IDictionary<string, object?> state,
        IDictionary<string, object> obsoletePersistenceModeConfiguration,
        IDictionary<string, LogPersistenceConfiguration> persistenceStrategyConfiguration,
        LogPersistenceMode defaultLogPersistenceMode,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, object?>();

        foreach (var value in state)
        {
            var propKey = value.Key.Camelize();
            var logPersistenceConfig = persistenceStrategyConfiguration.GetValueOrDefault(propKey, () => null);
            var mode = defaultLogPersistenceMode;

            if (logPersistenceConfig != null)
            {
                if (logPersistenceConfig.EvaluationMode == LogPersistenceEvaluationMode.Strategy)
                {
                    var strategyTypeName = logPersistenceConfig.StrategyType ?? typeof(Inherit).GetSimpleAssemblyQualifiedName();
                    var strategy = _logPersistenceStrategies.TryGetValue(strategyTypeName, out var v) ? v : new Inherit();
                    var strategyContext = new LogPersistenceStrategyContext(cancellationToken);
                    mode = await strategy.GetPersistenceModeAsync(strategyContext);
                }
                else
                {
                    var expression = logPersistenceConfig.Expression ?? Expression.LiteralExpression("Inherit");
                    mode = await _expressionEvaluator.EvaluateAsync<LogPersistenceMode>(expression, activityExecutionContext.ExpressionExecutionContext);
                }
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

    private static ActivityStatus GetAggregateStatus(ActivityExecutionContext context)
    {
        // If any child activity is faulted, the aggregate status is faulted.
        var descendantContexts = context.GetDescendants().ToList();

        if (descendantContexts.Any(x => x.Status == ActivityStatus.Faulted))
            return ActivityStatus.Faulted;

        return context.Status;
    }
    
    private static IDictionary<string, object> GetPayload(ActivityExecutionContext source)
    {
        var outcomes = source.JournalData.TryGetValue("Outcomes", out var resultValue) ? resultValue as string[] : default;
        var payload = new Dictionary<string, object>();

        if (outcomes != null)
            payload.Add("Outcomes", outcomes);
        
        return payload;
    }

    // Get the outputs of the activity execution context.
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

            if (cachedValue != default)
                return cachedValue;

            if (x.ValueGetter(activity) is Output output && source.TryGet(output.MemoryBlockReference(), out var outputValue))
                return outputValue;

            return default;
        });
        
        return outputs;
    }
}