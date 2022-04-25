using Elsa.Models;

namespace Elsa.Services;

public interface IScheduledNodeExecuted
{
    ValueTask HandleAsync(ActivityExecutionContext context, IActivity owner);
}