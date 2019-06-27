using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Results;
using Elsa.Serialization.Models;
using Elsa.Services.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Services
{
    public abstract class ActivityBase : IActivity
    {
        private readonly JsonSerializer serializer;

        protected ActivityBase()
        {
            serializer = new JsonSerializer().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        }
        
        public JObject State { get; set; } = new JObject();
        public virtual string TypeName => GetType().Name;
        public string Id { get; set; }

        public Task<bool> CanExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken) => OnCanExecuteAsync(context, cancellationToken);
        public Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken) => OnExecuteAsync(context, cancellationToken);
        public Task<ActivityExecutionResult> HaltedAsync(WorkflowExecutionContext context, CancellationToken cancellationToken) => OnHaltedAsync(context, cancellationToken);

        public ActivityInstance ToInstance() => new ActivityInstance
        {
            Id = Id,
            TypeName = TypeName,
            State = new JObject(State)
        };

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

        protected T GetState<T>(Func<T> defaultValue = null, [CallerMemberName] string name = null)
        {
            return State.GetState(name, defaultValue);
        }

        protected T GetState<T>(Type type, Func<T> defaultValue = null, [CallerMemberName] string name = null)
        {
            return State.GetState(type, name, defaultValue);
        }

        protected void SetState(object value, [CallerMemberName] string name = null)
        {
            State.SetState(name, value);
        }
    }
}