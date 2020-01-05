using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services.Models;
using Microsoft.Extensions.Localization;
using ScheduledActivity = Elsa.Services.Models.ScheduledActivity;

namespace Elsa.Services
{
    public abstract class Activity : IActivity
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

        public Task<bool> CanExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken) => OnCanExecuteAsync(workflowExecutionContext, activityExecutionContext, cancellationToken);
        public Task<IActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken) => OnExecuteAsync(workflowExecutionContext, activityExecutionContext, cancellationToken);

        public Task<IActivityExecutionResult> ResumeAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken) => OnResumeAsync(workflowExecutionContext, activityExecutionContext, cancellationToken);
        public Task<IActivityExecutionResult> ChildActivityExecutedAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken) => OnChildExecutedAsync(workflowExecutionContext, activityExecutionContext, cancellationToken);
        protected virtual bool OnCanExecute(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext) => true;
        protected virtual Task<bool> OnCanExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken) => Task.FromResult(OnCanExecute(workflowExecutionContext, activityExecutionContext));
        protected virtual Task<IActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken) => Task.FromResult(OnExecute(workflowExecutionContext, activityExecutionContext));
        protected virtual Task<IActivityExecutionResult> OnResumeAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken) => Task.FromResult(OnResume(workflowExecutionContext, activityExecutionContext));

        protected virtual Task<IActivityExecutionResult> OnChildExecutedAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            var result = OnChildExecuted(workflowExecutionContext, activityExecutionContext);
            return Task.FromResult(result);
        }

        protected virtual IActivityExecutionResult OnExecute(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext) => Done();
        protected virtual IActivityExecutionResult OnResume(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext) => Done();

        protected virtual IActivityExecutionResult OnChildExecuted(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext) => Noop();

        protected T GetState<T>(Func<T>? defaultValue = null, [CallerMemberName] string? name = null) => State.GetState(name, defaultValue);
        protected void SetState(object value, [CallerMemberName] string? name = null) => State.SetState(value, name);
        protected NoopResult Noop() => new NoopResult();
        protected OutcomeResult Done() => new OutcomeResult();
        protected OutcomeResult Done(string outcome, Variable output) => Done(new[] { outcome }, output);
        protected OutcomeResult Done(IEnumerable<string> outcomes, Variable output) => new OutcomeResult(outcomes, output);
        protected OutcomeResult Done(IEnumerable<string> outcomes, object output) => Done(outcomes, Variable.From(output));
        protected OutcomeResult Done(IEnumerable<string> outcomes) => Done(outcomes, default);
        protected OutcomeResult Done(params string[] outcomes) => Done(outcomes, default);
        protected OutcomeResult Done(Variable? output) => new OutcomeResult(null, output);
        protected SuspendResult Suspend() => new SuspendResult();
        protected ScheduleActivitiesResult Schedule(params IActivity[] activities) => new ScheduleActivitiesResult(activities);
        protected ScheduleActivitiesResult Schedule(IEnumerable<IActivity> activities, Variable input) => new ScheduleActivitiesResult(activities, input);
        protected ScheduleActivitiesResult Schedule(IEnumerable<IActivity> activities, object input) => new ScheduleActivitiesResult(activities, Variable.From(input));
        protected ScheduleActivitiesResult Schedule(IActivity activity, object input) => Schedule(new[] { activity }, input);
        protected ScheduleActivitiesResult Schedule(IActivity activity, Variable input) => Schedule(new[] { activity }, input);
        protected ScheduleActivitiesResult Schedule(IEnumerable<ScheduledActivity> activities) => new ScheduleActivitiesResult(activities);
        protected FaultResult Fault(LocalizedString message) => new FaultResult(message);
    }
}