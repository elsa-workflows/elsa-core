using System;
using Elsa.Models;

namespace Elsa.Services.Models
{
    public class LoadWorkflowContext
    {
        public LoadWorkflowContext(IWorkflowBlueprint workflowBlueprint, WorkflowInstance workflowInstance)
        {
            WorkflowBlueprint = workflowBlueprint;
            WorkflowInstance = workflowInstance;
        }

        public IWorkflowBlueprint WorkflowBlueprint { get; }
        public WorkflowInstance WorkflowInstance { get; }
        public Type ContextType => WorkflowBlueprint.ContextType!;
        public string ContextId => WorkflowInstance.ContextId!;

    }
}