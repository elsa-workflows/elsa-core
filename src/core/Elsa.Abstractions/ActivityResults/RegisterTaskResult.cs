using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.ActivityResults
{
    /// <summary>
    /// Registers a task to be executed after the workflow is suspended.
    /// </summary>
    public class RegisterTaskResult : ActivityExecutionResult
    {
        public Func<WorkflowExecutionContext, CancellationToken, ValueTask> Task { get; }
        public RegisterTaskResult(Func<WorkflowExecutionContext, CancellationToken, ValueTask> task) => Task = task;
        protected override void Execute(ActivityExecutionContext activityExecutionContext) => activityExecutionContext.WorkflowExecutionContext.RegisterTask(Task);
    }
}