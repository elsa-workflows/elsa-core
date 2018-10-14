using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.ActivityResults;
using Flowsharp.Models;

namespace Flowsharp.Activities
{
    public abstract class Activity : IActivity
    {
        public virtual Task<bool> CanExecuteAsync(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(CanExecute(workflowContext, activityContext));
        }

        public virtual Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(Execute(workflowContext, activityContext));
        }

        public virtual Task<ActivityExecutionResult> ResumeAsync(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(Resume(workflowContext, activityContext));
        }

        public virtual Task OnActivityExecutedAsync(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext, CancellationToken cancellationToken)
        {
            OnActivityExecuted(workflowContext, activityContext);
            return Task.CompletedTask;
        }

        public virtual Task OnActivityExecutingAsync(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext, CancellationToken cancellationToken)
        {
            OnActivityExecuting(workflowContext, activityContext);
            return  Task.CompletedTask;    
        }

        public virtual Task ReceiveInputAsync(WorkflowExecutionContext workflowContext, IDictionary<string, object> input, CancellationToken cancellationToken)
        {
            ReceiveInput(workflowContext, input);
            return Task.CompletedTask;
        }

        public virtual Task WorkflowResumedAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            WorkflowResumed(workflowContext);
            return Task.CompletedTask;
        }

        public virtual Task WorkflowResumingAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            WorkflowResuming(workflowContext);
            return Task.CompletedTask;
        }

        public virtual Task WorkflowStartedAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            WorkflowStarted(workflowContext);
            return Task.CompletedTask;
        }

        public virtual Task WorkflowStartingAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            WorkflowStarting(workflowContext);
            return Task.CompletedTask;
        }

        protected virtual ActivityExecutionResult Execute(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext)
        {
            return ActivateEndpoint();
        }
        
        protected virtual ActivityExecutionResult Resume(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext)
        {
            return ActivateEndpoint();
        }

        protected virtual bool CanExecute(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext) => true;
        protected virtual void OnActivityExecuted(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext) {}
        protected virtual void OnActivityExecuting(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext) {}
        protected virtual void ReceiveInput(WorkflowExecutionContext workflowContext, IDictionary<string, object> input) {}
        protected virtual void WorkflowResumed(WorkflowExecutionContext workflowContext) {}
        protected virtual void WorkflowResuming(WorkflowExecutionContext workflowContext) {}
        protected virtual void WorkflowStarted(WorkflowExecutionContext workflowContext) {}
        protected virtual void WorkflowStarting(WorkflowExecutionContext workflowContext) {}
        
        protected HaltResult Halt()
        {
            return new HaltResult();
        }

        protected ActivateEndpointResult ActivateEndpoint(string name = null)
        {
            return new ActivateEndpointResult(new SourceEndpoint(this, name));
        }

        protected ScheduleActivityResult ScheduleActivity(IActivity activity)
        {
            return new ScheduleActivityResult(activity);
        }
        
        protected ReturnValueResult SetReturnValue(object value)
        {
            return new ReturnValueResult(value);
        }

        protected FinishWorkflowResult Finish()
        {
            return new FinishWorkflowResult();
        }
        
        protected NoopResult Noop()
        {
            return new NoopResult();
        }
    }
}
