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
        public Type ActivityType => typeof(T);
        public virtual LocalizedString DisplayText => new LocalizedString(ActivityType.Name, ActivityType.Name);
        public virtual LocalizedString Description => new LocalizedString("", "");
        public Task<bool> CanExecuteAsync(IActivity activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => OnCanExecuteAsync((T)activity, workflowContext, cancellationToken);
        public Task<ActivityExecutionResult> ExecuteAsync(IActivity activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => OnExecuteAsync((T) activity, workflowContext, cancellationToken); 
        public Task<ActivityExecutionResult> ResumeAsync(IActivity activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => OnResumeAsync((T)activity, workflowContext, cancellationToken);
        protected virtual Task<bool> OnCanExecuteAsync(T activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => Task.FromResult(OnCanExecute(activity, workflowContext));
        protected virtual Task<ActivityExecutionResult> OnExecuteAsync(T activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => Task.FromResult(OnExecute(activity, workflowContext));
        protected virtual Task<ActivityExecutionResult> OnResumeAsync(T activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => Task.FromResult(OnResume(activity, workflowContext));
        protected virtual bool OnCanExecute(T activity, WorkflowExecutionContext workflowContext) => true;
        protected virtual ActivityExecutionResult OnExecute(T activity, WorkflowExecutionContext workflowContext) => Noop();
        protected virtual ActivityExecutionResult OnResume(T activity, WorkflowExecutionContext workflowContext) => Noop();
        protected NoopResult Noop() => new NoopResult();
        protected IEnumerable<LocalizedString> Endpoints(params LocalizedString[] endpoints) => endpoints;
        public virtual IEnumerable<LocalizedString> GetEndpoints() => Endpoints(GetEndpoint());
        protected virtual LocalizedString GetEndpoint() => throw new NotImplementedException("At least GetEndpoints or GetEndpoint must be overridden.");
    }
}