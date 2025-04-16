namespace Elsa.Workflows.Runtime;

public interface IActivityPropertyLogPersistenceEvaluator
{
    Task<ActivityLogPersistenceModeMap> EvaluateLogPersistenceModesAsync(ActivityExecutionContext context);
    
    Task<Dictionary<string, object?>> GetPersistableOutputAsync(ActivityExecutionContext context);
}