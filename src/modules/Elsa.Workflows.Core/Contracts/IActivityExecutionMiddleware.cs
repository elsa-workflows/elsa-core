using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Contracts;

public interface IActivityExecutionMiddleware
{
    ValueTask InvokeAsync(ActivityExecutionContext context);
}

