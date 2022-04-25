using Elsa.Models;

namespace Elsa.Services;

public interface IScopeCompletedHandler
{
    ValueTask HandleAsync(ActivityExecutionContext context);
}