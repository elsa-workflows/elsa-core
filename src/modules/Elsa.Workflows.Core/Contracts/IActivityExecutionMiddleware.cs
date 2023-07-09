namespace Elsa.Workflows.Core.Contracts;

public interface IActivityExecutionMiddleware
{
    ValueTask InvokeAsync(ActivityExecutionContext context);
}

