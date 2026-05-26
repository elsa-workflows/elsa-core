using Elsa.Workflows.Pipelines.ActivityExecution;

namespace Elsa.Workflows;

/// <summary>
/// Contributes middleware to the activity execution pipeline.
/// </summary>
public interface IActivityExecutionPipelineContributor
{
    /// <summary>
    /// Gets the order in which the contributor is applied.
    /// </summary>
    int Order => 0;

    /// <summary>
    /// Configures the activity execution pipeline.
    /// </summary>
    void Configure(IActivityExecutionPipelineBuilder builder);
}
