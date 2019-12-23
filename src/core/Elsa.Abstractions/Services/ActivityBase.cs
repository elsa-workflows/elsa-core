using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public abstract class ActivityBase : IActivity
    {
        public Variables State { get; set; } = new Variables();
        public Variable? Output { get; set; }

        public virtual string Type => GetType().Name;

        public string? Id { get; set; }

        [ActivityProperty(Label = "Name", Hint = "Optionally provide a name for this activity. You can reference named activities from expressions.")]
        public string? Name
        {
            get => GetState<string>();
            set => SetState(value);
        }

        [ActivityProperty(Label = "Display Name", Hint = "Optionally provide a display name for this activity.")]
        public string? DisplayName
        {
            get => GetState<string>();
            set => SetState(value);
        }

        [ActivityProperty(Hint = "Optionally provide a custom title for this activity.")]
        public string? Title
        {
            get => GetState<string>();
            set => SetState(value);
        }

        [ActivityProperty(Hint = "Optionally provide a custom description for this activity.")]
        public string? Description
        {
            get => GetState<string>();
            set => SetState(value);
        }

        public Task<bool> CanExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken) => OnCanExecuteAsync(context, cancellationToken);
        public Task<IActivityExecutionResult> ExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken) => OnExecuteAsync(context, cancellationToken);

        public ActivityInstance ToInstance() => new ActivityInstance
        {
            Id = Id,
            Type = Type,
            State = new Variables(State),
            Output = Output
        };

        public Task<IActivityExecutionResult> ResumeAsync(ActivityExecutionContext context, CancellationToken cancellationToken) => OnResumeAsync(context, cancellationToken);
        protected virtual bool OnCanExecute(ActivityExecutionContext context) => true;
        protected virtual Task<bool> OnCanExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken) => Task.FromResult(OnCanExecute(context));
        protected virtual Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken) => Task.FromResult(OnExecute(context));
        protected virtual Task<IActivityExecutionResult> OnResumeAsync(ActivityExecutionContext context, CancellationToken cancellationToken) => Task.FromResult(OnResume(context));
        protected virtual IActivityExecutionResult OnExecute(ActivityExecutionContext context) => Noop();
        protected virtual IActivityExecutionResult OnResume(ActivityExecutionContext context) => Noop();
        protected NoopResult Noop() => new NoopResult();

        protected T GetState<T>(Func<T>? defaultValue = null, [CallerMemberName] string? name = null)
        {
            return State.GetState(name, defaultValue);
        }

        protected void SetState(object value, [CallerMemberName] string? name = null)
        {
            State.SetState(value, name);
        }
    }
}