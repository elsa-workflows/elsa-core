using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Flowsharp.Results;
using Microsoft.Extensions.Localization;

namespace Flowsharp.Handlers
{
    public abstract class ActivityHandlerBase<T> : IActivityHandler where T : IActivity
    {
        public string ActivityName => typeof(T).Name;
        public virtual LocalizedString DisplayText => new LocalizedString(ActivityName, ActivityName);
        public virtual LocalizedString Description => new LocalizedString("", "");
        public Task<bool> CanExecuteAsync(ActivityExecutionContext activityContext, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => OnCanExecuteAsync((ActivityExecutionContext<T>) activityContext, workflowContext, cancellationToken);
        public Task<ActivityExecutionResult> ExecuteAsync(ActivityExecutionContext activityContext, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => OnExecuteAsync((ActivityExecutionContext<T>) activityContext, workflowContext, cancellationToken); 
        public Task<ActivityExecutionResult> ResumeAsync(ActivityExecutionContext activityContext, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => OnResumeAsync((ActivityExecutionContext<T>) activityContext, workflowContext, cancellationToken);
        protected virtual Task<bool> OnCanExecuteAsync(ActivityExecutionContext<T> activityContext, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => OnCanExecuteAsync((T)activityContext.Activity, workflowContext, cancellationToken);
        protected virtual Task<bool> OnCanExecuteAsync(T activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => Task.FromResult(OnCanExecute(activity, workflowContext));
        protected virtual Task<ActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext<T> activityContext, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => OnExecuteAsync(activityContext.Activity, workflowContext, cancellationToken);
        protected virtual Task<ActivityExecutionResult> OnExecuteAsync(T activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => Task.FromResult(OnExecute(activity, workflowContext));
        protected virtual Task<ActivityExecutionResult> OnResumeAsync(ActivityExecutionContext<T> activityContext, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => Task.FromResult(OnResume(activityContext, workflowContext));
        protected virtual Task<ActivityExecutionResult> OnResumeAsync(T activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => Task.FromResult(OnResume(activity, workflowContext));
        protected virtual bool OnCanExecute(ActivityExecutionContext<T> activityContext, WorkflowExecutionContext workflowContext) => OnCanExecute(activityContext.Activity, workflowContext);
        protected virtual bool OnCanExecute(T activity, WorkflowExecutionContext workflowContext) => true;
        protected virtual ActivityExecutionResult OnExecute(ActivityExecutionContext<T> activityContext, WorkflowExecutionContext workflowContext) => OnExecute(activityContext.Activity, workflowContext);
        protected virtual ActivityExecutionResult OnExecute(T activity, WorkflowExecutionContext workflowContext) => Noop();
        protected virtual ActivityExecutionResult OnResume(ActivityExecutionContext<T> activityContext, WorkflowExecutionContext workflowContext) => OnResume(activityContext.Activity, workflowContext);
        protected virtual ActivityExecutionResult OnResume(T activity, WorkflowExecutionContext workflowContext) => Noop();
        protected NoopResult Noop() => new NoopResult();
        protected IEnumerable<LocalizedString> Endpoints(params LocalizedString[] endpoints) => endpoints;
        public virtual IEnumerable<LocalizedString> GetEndpoints() => Endpoints(GetEndpoint());
        protected virtual LocalizedString GetEndpoint() => throw new NotImplementedException("At least GetEndpoints or GetEndpoint must be overridden.");
    }
}