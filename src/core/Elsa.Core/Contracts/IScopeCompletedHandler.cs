using Elsa.Models;

namespace Elsa.Contracts;

public interface IScopeCompletedHandler
{
    ValueTask HandleAsync(ActivityExecutionContext context);
}