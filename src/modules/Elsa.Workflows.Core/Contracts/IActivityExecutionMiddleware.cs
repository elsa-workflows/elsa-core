namespace Elsa.Workflows.Contracts;

public interface IActivityExecutionMiddleware
{
    ValueTask InvokeAsync(ActivityExecutionContext context);
}

