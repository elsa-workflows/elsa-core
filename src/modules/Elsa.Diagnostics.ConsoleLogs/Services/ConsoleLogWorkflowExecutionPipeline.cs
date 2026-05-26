namespace Elsa.Diagnostics.ConsoleLogs.Services;

/// <summary>
/// Adds console log metadata for workflow execution.
/// </summary>
public sealed class ConsoleLogWorkflowExecutionPipeline(
    IWorkflowExecutionPipeline inner,
    IConsoleLogContextAccessor consoleLogContextAccessor) : IWorkflowExecutionPipeline
{
    public Action<IWorkflowExecutionPipelineBuilder> ConfigurePipelineBuilder => inner.ConfigurePipelineBuilder;
    public WorkflowMiddlewareDelegate Pipeline => inner.Pipeline;
    public WorkflowMiddlewareDelegate Setup(Action<IWorkflowExecutionPipelineBuilder> setup) => inner.Setup(setup);

    public async Task ExecuteAsync(WorkflowExecutionContext context)
    {
        using var scope = consoleLogContextAccessor.PushWorkflowInstanceId(context.Id);
        await inner.ExecuteAsync(context);
    }
}
