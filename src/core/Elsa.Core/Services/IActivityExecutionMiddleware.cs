using Elsa.Models;

namespace Elsa.Services;

public interface IActivityExecutionMiddleware
{
    ValueTask InvokeAsync(ActivityExecutionContext context);
}