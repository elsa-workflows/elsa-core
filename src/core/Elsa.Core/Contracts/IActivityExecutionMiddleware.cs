using Elsa.Models;

namespace Elsa.Contracts;

public interface IActivityExecutionMiddleware
{
    ValueTask InvokeAsync(ActivityExecutionContext context);
}