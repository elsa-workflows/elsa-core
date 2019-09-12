using System.Collections.Generic;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowRegistry
    {
        void RegisterWorkflow(WorkflowDefinitionVersion definition);
        WorkflowDefinitionVersion RegisterWorkflow<T>() where T:IWorkflow, new();
        IEnumerable<(WorkflowDefinitionVersion, ActivityDefinition)> ListByStartActivity(string activityType);
        WorkflowDefinitionVersion GetById(string id, int version);
    }
}