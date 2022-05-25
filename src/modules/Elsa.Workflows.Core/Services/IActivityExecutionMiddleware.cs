using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Services;

public interface IActivityExecutionMiddleware
{
    ValueTask InvokeAsync(ActivityExecutionContext context);
}