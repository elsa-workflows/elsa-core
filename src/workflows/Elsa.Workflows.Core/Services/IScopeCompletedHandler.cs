using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Services;

public interface IScopeCompletedHandler
{
    ValueTask HandleAsync(ActivityExecutionContext context);
}