using System.Threading.Tasks;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Services;

public interface IScheduledNodeExecuted
{
    ValueTask HandleAsync(ActivityExecutionContext context, IActivity owner);
}