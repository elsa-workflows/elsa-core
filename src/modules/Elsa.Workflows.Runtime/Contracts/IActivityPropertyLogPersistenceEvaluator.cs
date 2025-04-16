using Elsa.Expressions.Models;
using Elsa.Workflows.LogPersistence;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime;

public interface IActivityPropertyLogPersistenceEvaluator
{
    Task<ActivityLogPersistenceModeMap> EvaluateLogPersistenceModesAsync(ActivityExecutionContext context);
    Task<LogPersistenceMode> EvaluateLogPersistenceModeAsync(ActivityExecutionContext context, PropertyDescriptor propertyDescriptor);
    
    Task<Dictionary<string, object?>> GetPersistableOutputAsync(ActivityExecutionContext context);

    Task<Dictionary<string, object?>> GetPersistableOutputAsync(
        ActivityExecutionContext context,
        IDictionary<string, object?> legacyActivityPersistenceProperties,
        IDictionary<string, object?> activityPersistenceProperties,
        LogPersistenceMode activityPersistencePropertyDefault,
        CancellationToken cancellationToken = default);

    Task<Dictionary<string, object?>> GetPersistableInputAsync(ActivityExecutionContext context,
        IDictionary<string, object?> legacyActivityPersistenceProperties,
        IDictionary<string, object?> activityPersistenceProperties,
        LogPersistenceMode activityPersistencePropertyDefault,
        CancellationToken cancellationToken = default);

    Task<Dictionary<string, object?>> GetPersistablePropertiesAsync(
        ActivityExecutionContext context,
        IDictionary<string, object?> state,
        string key,
        IDictionary<string, object?> legacyActivityPersistenceProperties,
        IDictionary<string, object?> activityPersistenceProperties,
        LogPersistenceMode activityPersistencePropertyDefault,
        CancellationToken cancellationToken = default);

    Task<LogPersistenceMode> GetDefaultPersistenceModeAsync(
        ExpressionExecutionContext expressionExecutionContext, 
        IDictionary<string, object> customProperties, 
        Func<LogPersistenceMode> defaultFactory, 
        CancellationToken cancellationToken = default);

    Task<Dictionary<string, object?>> FilterPropertiesUsingPersistenceMode(
        ExpressionExecutionContext expressionExecutionContext,
        IDictionary<string, object?> state,
        IDictionary<string, object> obsoletePersistenceModeConfiguration,
        IDictionary<string, object> persistenceStrategyConfiguration,
        LogPersistenceMode defaultLogPersistenceMode,
        CancellationToken cancellationToken = default);

    Task<LogPersistenceMode> EvaluateLogPersistenceConfigAsync(
        LogPersistenceConfiguration? config, 
        ExpressionExecutionContext executionContext, 
        Func<LogPersistenceMode> defaultMode, 
        CancellationToken cancellationToken = default);
}