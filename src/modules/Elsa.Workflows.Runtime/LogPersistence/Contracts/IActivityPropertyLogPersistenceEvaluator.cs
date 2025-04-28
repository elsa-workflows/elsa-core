namespace Elsa.Workflows.Runtime;

/// <summary>
/// Provides functionality for evaluating log persistence settings for activity properties
/// during the execution of a workflow.
/// </summary>
public interface IActivityPropertyLogPersistenceEvaluator
{
    /// <summary>
    /// Evaluates the log persistence settings for activity properties within the context of a workflow's execution.
    /// </summary>
    Task<ActivityLogPersistenceModeMap> EvaluateLogPersistenceModesAsync(ActivityExecutionContext context);

    /// <summary>
    /// Retrieves a dictionary of persistable output values generated during the execution of an activity.
    /// </summary>
    /// <returns>A dictionary where the keys represent output property names and the values represent their persistable data.</returns>
    Task<Dictionary<string, object>> GetPersistableOutputAsync(ActivityExecutionContext context);
}