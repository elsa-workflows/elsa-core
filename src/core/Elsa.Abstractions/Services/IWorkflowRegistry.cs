using System.Collections.Generic;
using System.Threading;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowRegistry
    {
        void RegisterWorkflow(WorkflowDefinition definition);
        IEnumerable<(WorkflowDefinition, ActivityDefinition)> ListByStartActivity(string activityType, CancellationToken cancellationToken = default);
        WorkflowDefinition GetById(string id, CancellationToken cancellationToken = default);
    }
}