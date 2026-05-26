using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Diagnostics.ConsoleLogs.Extensions;

public static class ConsoleLogContextWorkflowPipelineBuilderExtensions
{
    public static IWorkflowExecutionPipelineBuilder UseConsoleLogContext(this IWorkflowExecutionPipelineBuilder builder)
    {
        return builder.Use(next => async context =>
        {
            var consoleLogContextAccessor = builder.ServiceProvider.GetRequiredService<IConsoleLogContextAccessor>();
            using var scope = consoleLogContextAccessor.PushWorkflowInstanceId(context.Id);
            await next(context);
        });
    }

    public static IActivityExecutionPipelineBuilder UseConsoleLogContext(this IActivityExecutionPipelineBuilder builder)
    {
        return builder.Use(next => async context =>
        {
            var consoleLogContextAccessor = builder.ServiceProvider.GetRequiredService<IConsoleLogContextAccessor>();
            using var scope = consoleLogContextAccessor.PushWorkflowInstanceId(context.WorkflowExecutionContext.Id);
            await next(context);
        });
    }
}
