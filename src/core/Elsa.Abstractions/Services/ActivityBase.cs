using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Results;
using Elsa.Services.Models;
using Newtonsoft.Json.Linq;

namespace Elsa.Services
{
    public abstract class ActivityBase : IActivity
    {
        public JObject State { get; set; } = new JObject();
        public virtual string TypeName => GetType().Name;

        public string Id { get; set; }
        
        public Task<bool> CanExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken) => OnCanExecuteAsync(context, cancellationToken);
        public Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken) => OnExecuteAsync(context, cancellationToken);
        public Task<ActivityExecutionResult> HaltedAsync(WorkflowExecutionContext context, CancellationToken cancellationToken) => OnHaltedAsync(context, cancellationToken);
        public Task<ActivityExecutionResult> ResumeAsync(WorkflowExecutionContext context, CancellationToken cancellationToken) => OnResumeAsync(context, cancellationToken);
        protected virtual Task<bool> OnCanExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken) => Task.FromResult(OnCanExecute(context));
        protected virtual Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken) => Task.FromResult(OnExecute(context));
        protected virtual Task<ActivityExecutionResult> OnHaltedAsync(WorkflowExecutionContext context, CancellationToken cancellationToken) => Task.FromResult(OnHalted(context));
        protected virtual Task<ActivityExecutionResult> OnResumeAsync(WorkflowExecutionContext context, CancellationToken cancellationToken) => Task.FromResult(OnResume(context));
        protected virtual bool OnCanExecute(WorkflowExecutionContext context) => true;
        protected virtual ActivityExecutionResult OnExecute(WorkflowExecutionContext context) => Noop();
        protected virtual ActivityExecutionResult OnHalted(WorkflowExecutionContext context) => Noop();
        protected virtual ActivityExecutionResult OnResume(WorkflowExecutionContext context) => Noop();
        protected NoopResult Noop() => new NoopResult();
        
        protected T GetState<T>(Func<T> defaultValue = null, [CallerMemberName]string name = null)
        {
            var item = State[name];
            return item != null ? item.ToObject<T>() : defaultValue != null ? defaultValue() : default;
        }

        protected T GetState<T>(Type type, Func<T> defaultValue = null, [CallerMemberName]string name = null)
        {
            var item = State[name];
            return item != null ? (T)item.ToObject(type) : defaultValue != null ? defaultValue() : default;
        }

        protected void SetState(object value, [CallerMemberName]string name = null)
        {
            State[name] = JToken.FromObject(value);
        }
    }
}
