using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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
        public string? Name { get; set; }
        public string? DisplayName{ get; set; }
        public string? Description{ get; set; }
        public bool PersistWorkflow { get; set; }

        public Task<bool> CanExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken) => OnCanExecuteAsync(context, cancellationToken);
        public Task<IActivityExecutionResult> ExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken) => OnExecuteAsync(context, cancellationToken);

        public Task<IActivityExecutionResult> ResumeAsync(ActivityExecutionContext context, CancellationToken cancellationToken) => OnResumeAsync(context, cancellationToken);
        protected virtual bool OnCanExecute(ActivityExecutionContext context) => true;
        protected virtual Task<bool> OnCanExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken) => Task.FromResult(OnCanExecute(context));
        protected virtual Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken) => Task.FromResult(OnExecute(context));
        protected virtual Task<IActivityExecutionResult> OnResumeAsync(ActivityExecutionContext context, CancellationToken cancellationToken) => Task.FromResult(OnResume(context));
        protected virtual IActivityExecutionResult OnExecute(ActivityExecutionContext context) => Done();
        protected virtual IActivityExecutionResult OnResume(ActivityExecutionContext context) => Done();
        protected T GetState<T>(Func<T>? defaultValue = null, [CallerMemberName] string? name = null) => State.GetState(name, defaultValue);
        protected void SetState(object? value, [CallerMemberName] string? name = null) => State.SetState(value, name);
        protected NoopResult Noop() => new NoopResult();
        protected OutcomeResult Done() => new OutcomeResult();
        protected OutcomeResult Done(string outcome, Variable? output) => Done(new[] { outcome }, output);
        protected OutcomeResult Done(IEnumerable<string> outcomes, Variable? output) => new OutcomeResult(outcomes, output);
        protected OutcomeResult Done(IEnumerable<string> outcomes, object? output) => Done(outcomes, Variable.From(output));
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
        protected CombinedResult Combine(IEnumerable<IActivityExecutionResult> results) => new CombinedResult(results);
        protected CombinedResult Combine(params IActivityExecutionResult[] results) => new CombinedResult(results);
        protected FaultResult Fault(LocalizedString message) => new FaultResult(message);
    }
}