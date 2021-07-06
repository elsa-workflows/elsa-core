using System;

namespace Elsa.Services.Models
{
    public class SaveWorkflowContext
    {
        public SaveWorkflowContext(WorkflowExecutionContext workflowExecutionContext)
        {
            WorkflowExecutionContext = workflowExecutionContext;
        }

        public WorkflowExecutionContext WorkflowExecutionContext { get; }
        public Type ContextType => WorkflowExecutionContext.WorkflowBlueprint.ContextOptions!.ContextType!;
        public string ContextId => WorkflowExecutionContext.WorkflowInstance.ContextId!;
        public object? Context => WorkflowExecutionContext.WorkflowContext; 
    }
    
    public class SaveWorkflowContext<T> : SaveWorkflowContext
    {
        public SaveWorkflowContext(WorkflowExecutionContext workflowExecutionContext) : base(workflowExecutionContext)
        {
        }

        public SaveWorkflowContext(SaveWorkflowContext context) : base(context.WorkflowExecutionContext)
        {
        }
        
        public new T? Context => (T?)base.Context;

    }
}