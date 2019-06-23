using System.Collections.Generic;
using System.Threading;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowRegistry
    {
        void RegisterWorkflow(WorkflowBlueprint blueprint);
        IEnumerable<(WorkflowBlueprint, ActivityBlueprint)> ListByStartActivity(string activityType, CancellationToken cancellationToken = default);
        WorkflowBlueprint GetById(string id, CancellationToken cancellationToken = default);
    }
}