using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.ActivityResults;
using Flowsharp.Models;
using Microsoft.Extensions.Localization;

namespace Flowsharp.Activities
{
    public abstract class Activity : IActivity
    {
        public virtual string Name => GetType().Name;

        public Task ProvideMetadataAsync(ActivityMetadataContext context, CancellationToken cancellationToken)
        {
            ProvideMetadata(context);
            return Task.CompletedTask;
        }

        public virtual Task<bool> CanExecuteAsync(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public virtual Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(Execute(workflowContext, activityContext));
        }

        public virtual Task<ActivityExecutionResult> ResumeAsync(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(Resume(workflowContext, activityContext));
        }

        public virtual IEnumerable<Outcome> GetOutcomes(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext)
        {
            return Enumerable.Empty<Outcome>();
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

        protected virtual void ProvideMetadata(ActivityMetadataContext context)
        {
        }
        
        protected virtual ActivityExecutionResult Execute(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext)
        {
            return Noop();
        }
        
        protected virtual ActivityExecutionResult Resume(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext)
        {
            return Noop();
        }
        
        protected virtual void OnActivityExecuted(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext) {}
        protected virtual void OnActivityExecuting(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext) {}
        protected virtual void ReceiveInput(WorkflowExecutionContext workflowContext, IDictionary<string, object> input) {}
        protected virtual void WorkflowResumed(WorkflowExecutionContext workflowContext) {}
        protected virtual void WorkflowResuming(WorkflowExecutionContext workflowContext) {}
        protected virtual void WorkflowStarted(WorkflowExecutionContext workflowContext) {}
        protected virtual void WorkflowStarting(WorkflowExecutionContext workflowContext) {}
        
        protected IEnumerable<Outcome> Outcomes(params LocalizedString[] names)
        {
            return names.Select(x => new Outcome(x));
        }

        protected IEnumerable<Outcome> Outcomes(IEnumerable<LocalizedString> names)
        {
            return names.Select(x => new Outcome(x));
        }

        protected ActivityExecutionResult Outcomes(params string[] names)
        {
            return Outcomes((IEnumerable<string>)names);
        }

        protected ActivityExecutionResult Outcomes(IEnumerable<string> names)
        {
            return new OutcomeResult(names);
        }

        protected ActivityExecutionResult Halt()
        {
            return new HaltResult();
        }

        protected ActivityExecutionResult Noop()
        {
            return new NoopResult();
        }
    }
}
