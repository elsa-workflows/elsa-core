using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Primitives
{
    /// <summary>
    /// Run arbitrary .NET code from coded workflows.
    /// </summary>
    public class Inline : Activity
    {
        public Inline()
        {
            Function = (workflowExecutionContext, activityExecutionContext) => Task.FromResult<IActivityExecutionResult>(Done());
        }

        public Inline(Func<WorkflowExecutionContext, ActivityExecutionContext, Task<IActivityExecutionResult>> function)
        {
            Function = function;
        }
        
        public Inline(Func<WorkflowExecutionContext, ActivityExecutionContext, Task> function)
        {
            Function = (workflowExecutionContext, activityExecutionContext) =>
            {
                function(workflowExecutionContext, activityExecutionContext);
                return Task.FromResult<IActivityExecutionResult>(Done());
            };
        }
        
        public Inline(Action<WorkflowExecutionContext, ActivityExecutionContext> function)
        {
            Function = (workflowExecutionContext, activityExecutionContext) =>
            {
                function(workflowExecutionContext, activityExecutionContext);
                return Task.FromResult<IActivityExecutionResult>(Done());
            };
        }
        
        public Inline(Action function) : this((workflowExecutionContext, activityExecutionContext) => function())
        {
        }
        
        public Func<WorkflowExecutionContext, ActivityExecutionContext, Task<IActivityExecutionResult>> Function { get; set; }

        protected override Task<IActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken) => Function(workflowExecutionContext, activityExecutionContext);
    }
}