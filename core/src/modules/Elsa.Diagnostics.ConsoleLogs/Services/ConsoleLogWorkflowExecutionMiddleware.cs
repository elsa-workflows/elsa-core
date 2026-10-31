namespace Elsa.Diagnostics.ConsoleLogs.Services;

/// <summary>
/// Adds console log metadata for workflow execution.
/// </summary>
public sealed class ConsoleLogWorkflowExecutionMiddleware(
    WorkflowMiddlewareDelegate next,
    IConsoleLogContextAccessor consoleLogContextAccessor) : IWorkflowExecutionMiddleware
{
    public async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        using var scope = consoleLogContextAccessor.PushWorkflowExecution(context);
        await next(context);
    }
}
