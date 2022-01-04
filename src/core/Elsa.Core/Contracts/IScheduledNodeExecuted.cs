using Elsa.Models;

namespace Elsa.Contracts;

public interface IScheduledNodeExecuted
{
    ValueTask HandleAsync(ActivityExecutionContext context, IActivity owner);
}