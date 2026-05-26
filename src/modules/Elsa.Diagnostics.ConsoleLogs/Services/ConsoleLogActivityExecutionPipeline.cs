namespace Elsa.Diagnostics.ConsoleLogs.Services;

/// <summary>
/// Adds console log metadata for activity execution, including detached background activity execution.
/// </summary>
public sealed class ConsoleLogActivityExecutionPipeline(
    IActivityExecutionPipeline inner,
    IConsoleLogContextAccessor consoleLogContextAccessor) : IActivityExecutionPipeline
{
    public ActivityMiddlewareDelegate Pipeline => inner.Pipeline;
    public ActivityMiddlewareDelegate Setup(Action<IActivityExecutionPipelineBuilder> setup) => inner.Setup(setup);

    public async Task ExecuteAsync(ActivityExecutionContext context)
    {
        using var scope = consoleLogContextAccessor.PushWorkflowInstanceId(context.WorkflowExecutionContext.Id);
        await inner.ExecuteAsync(context);
    }
}
