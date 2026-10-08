namespace Elsa.Diagnostics.ConsoleLogs.Services;

/// <summary>
/// Installs console log metadata capture into workflow execution.
/// </summary>
public sealed class ConsoleLogWorkflowExecutionPipelineContributor : IWorkflowExecutionPipelineContributor
{
    public void Configure(IWorkflowExecutionPipelineBuilder builder)
    {
        builder.UseMiddleware<ConsoleLogWorkflowExecutionMiddleware>();
    }
}
