using System;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Flowsharp.Results;

namespace Flowsharp.Handlers
{
    public abstract class ActivityHandler<T> : IActivityHandler where T : IActivity
    {
        public Type ActivityType => typeof(T);
        
        public Task<bool> CanExecuteAsync(IActivity activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => OnCanExecuteAsync((T)activity, workflowContext, cancellationToken);
        public Task<ActivityExecutionResult> ExecuteAsync(IActivity activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => OnExecuteAsync((T) activity, workflowContext, cancellationToken); 
        public Task<ActivityExecutionResult> ResumeAsync(IActivity activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => OnResumeAsync((T)activity, workflowContext, cancellationToken);
        
        protected virtual Task<bool> OnCanExecuteAsync(T activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => Task.FromResult(OnCanExecute(activity, workflowContext));
        protected virtual Task<ActivityExecutionResult> OnExecuteAsync(T activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => Task.FromResult(OnExecute(activity, workflowContext));
        protected virtual Task<ActivityExecutionResult> OnResumeAsync(T activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => Task.FromResult(OnResume(activity, workflowContext));
        
        protected virtual bool OnCanExecute(T activity, WorkflowExecutionContext workflowContext) => true;
        protected virtual ActivityExecutionResult OnExecute(T activity, WorkflowExecutionContext workflowContext) => Noop();
        protected virtual ActivityExecutionResult OnResume(T activity, WorkflowExecutionContext workflowContext) => Noop();

        protected HaltResult Halt() => new HaltResult();
        protected TriggerEndpointResult TriggerEndpoint(string name = null) => new TriggerEndpointResult(name);
        protected ScheduleActivityResult ScheduleActivity(IActivity activity) => new ScheduleActivityResult(activity);
        protected ReturnValueResult SetReturnValue(object value) => new ReturnValueResult(value);
        protected FinishWorkflowResult Finish() => new FinishWorkflowResult();
        protected NoopResult Noop() => new NoopResult();
    }
}