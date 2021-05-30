using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Conductor.Models;

namespace Elsa.Activities.Conductor.Services
{
    public interface ITasksProvider
    {
        ValueTask<IEnumerable<TaskDefinition>> GetTasksAsync(CancellationToken cancellationToken);
    }
}