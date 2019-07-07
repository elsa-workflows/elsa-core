using System.Collections.Generic;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowRegistry
    {
        void RegisterWorkflow(WorkflowDefinition definition);
        void RegisterWorkflow<T>() where T:IWorkflow, new();
        IEnumerable<(WorkflowDefinition, ActivityDefinition)> ListByStartActivity(string activityType);
        WorkflowDefinition GetById(string id);
    }
}