using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Results;

namespace Elsa
{
    public abstract class ActivityDriverBase<T> : IActivityDriver where T : IActivity
    {
        public string ActivityType => typeof(T).Name;
        public Task<bool> CanExecuteAsync(ActivityExecutionContext activityContext, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => OnCanExecuteAsync(activityContext, workflowContext, cancellationToken);
        public Task<ActivityExecutionResult> ExecuteAsync(ActivityExecutionContext activityContext, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => OnExecuteAsync(activityContext, workflowContext, cancellationToken);
        public Task<ActivityExecutionResult> ResumeAsync(ActivityExecutionContext activityContext, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => OnResumeAsync(activityContext, workflowContext, cancellationToken);
        protected virtual Task<bool> OnCanExecuteAsync(ActivityExecutionContext activityContext, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => OnCanExecuteAsync((T) activityContext.Activity, workflowContext, cancellationToken);
        protected virtual Task<bool> OnCanExecuteAsync(T activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => Task.FromResult(OnCanExecute(activity, workflowContext));
        protected virtual Task<ActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext activityContext, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => OnExecuteAsync((T) activityContext.Activity, workflowContext, cancellationToken);
        protected virtual Task<ActivityExecutionResult> OnExecuteAsync(T activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => Task.FromResult(OnExecute(activity, workflowContext));
        protected virtual Task<ActivityExecutionResult> OnResumeAsync(ActivityExecutionContext activityContext, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => Task.FromResult(OnResume(activityContext, workflowContext));
        protected virtual Task<ActivityExecutionResult> OnResumeAsync(T activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => Task.FromResult(OnResume(activity, workflowContext));
        protected virtual bool OnCanExecute(ActivityExecutionContext activityContext, WorkflowExecutionContext workflowContext) => OnCanExecute((T) activityContext.Activity, workflowContext);
        protected virtual bool OnCanExecute(T activity, WorkflowExecutionContext workflowContext) => true;
        protected virtual ActivityExecutionResult OnExecute(ActivityExecutionContext activityContext, WorkflowExecutionContext workflowContext) => OnExecute((T) activityContext.Activity, workflowContext);
        protected virtual ActivityExecutionResult OnExecute(T activity, WorkflowExecutionContext workflowContext) => Noop();
        protected virtual ActivityExecutionResult OnResume(ActivityExecutionContext activityContext, WorkflowExecutionContext workflowContext) => OnResume((T) activityContext.Activity, workflowContext);
        protected virtual ActivityExecutionResult OnResume(T activity, WorkflowExecutionContext workflowContext) => Noop();
        protected NoopResult Noop() => new NoopResult();
    }
}