using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services.Models;
using Newtonsoft.Json.Linq;

namespace Elsa.Services
{
    public abstract class ActivityBase : IActivity
    {
        public JObject State { get; set; } = new JObject();
        public Variable Output { get; set; }

        public virtual string Type => GetType().Name;
        
        public string Id { get; set; }
        
        [ActivityProperty(Label = "Name", Hint = "Optionally provide a name for this activity. You can reference named activities from expressions.")]
        public string Name {
            get => GetState<string>();
            set => SetState(value);
        }

        [ActivityProperty(Hint = "Optionally provide a custom title for this activity.")]
        public string Title
        {
            get => GetState<string>();
            set => SetState(value);
        }

        [ActivityProperty(Hint = "Optionally provide a custom description for this activity.")]
        public string Description
        {
            get => GetState<string>();
            set => SetState(value);
        }

        public Task<bool> CanExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken) => OnCanExecuteAsync(context, cancellationToken);
        public Task<IActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken) => OnExecuteAsync(context, cancellationToken);

        public ActivityInstance ToInstance() => new ActivityInstance
        {
            Id = Id,
            Type = Type,
            State = new JObject(State),
            Output = Output != null ? JObject.FromObject(Output) : null
        };

        public Task<IActivityExecutionResult> ResumeAsync(WorkflowExecutionContext context, CancellationToken cancellationToken) => OnResumeAsync(context, cancellationToken);
        protected virtual Task<bool> OnCanExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken) => Task.FromResult(OnCanExecute(context));
        protected virtual Task<IActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken) => Task.FromResult(OnExecute(context));
        protected virtual Task<IActivityExecutionResult> OnResumeAsync(WorkflowExecutionContext context, CancellationToken cancellationToken) => Task.FromResult(OnResume(context));
        protected virtual bool OnCanExecute(WorkflowExecutionContext context) => true;
        protected virtual IActivityExecutionResult OnExecute(WorkflowExecutionContext context) => Noop();
        protected virtual IActivityExecutionResult OnResume(WorkflowExecutionContext context) => Noop();
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