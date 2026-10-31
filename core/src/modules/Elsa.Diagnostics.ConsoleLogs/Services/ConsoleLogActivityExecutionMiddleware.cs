namespace Elsa.Diagnostics.ConsoleLogs.Services;

/// <summary>
/// Adds console log metadata for activity execution, including detached background activity execution.
/// </summary>
public sealed class ConsoleLogActivityExecutionMiddleware(
    ActivityMiddlewareDelegate next,
    IConsoleLogContextAccessor consoleLogContextAccessor) : IActivityExecutionMiddleware
{
    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        using var scope = consoleLogContextAccessor.PushActivityExecution(context);
        await next(context);
    }
}
