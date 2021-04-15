using System;
using Elsa.Models;

namespace Elsa.Services.Models
{
    public class LoadWorkflowContext
    {
        public LoadWorkflowContext(WorkflowExecutionContext workflowExecutionContext)
        {
            WorkflowExecutionContext = workflowExecutionContext;
        }

        public WorkflowExecutionContext WorkflowExecutionContext { get; }
        public IWorkflowBlueprint WorkflowBlueprint => WorkflowExecutionContext.WorkflowBlueprint;
        public WorkflowInstance WorkflowInstance => WorkflowExecutionContext.WorkflowInstance;
        public Type? ContextType => WorkflowBlueprint.ContextOptions?.ContextType;
        public string ContextId => WorkflowInstance.ContextId!;
    }
}