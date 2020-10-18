using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Models;
using Elsa.Services.Models;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json.Linq;

namespace Elsa.Services
{
    public abstract class Activity : IActivity
    {
        public string Type => GetType().Name;
        public string Id { get; set; } = default!;
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public bool PersistWorkflow { get; set; }
        public JObject Data { get; set; } = default!;

        public ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken) =>
            OnCanExecuteAsync(context, cancellationToken);

        public ValueTask<IActivityExecutionResult> ExecuteAsync(
            ActivityExecutionContext context,
            CancellationToken cancellationToken) => OnExecuteAsync(context, cancellationToken);

        public ValueTask<IActivityExecutionResult> ResumeAsync(
            ActivityExecutionContext context,
            CancellationToken cancellationToken) => OnResumeAsync(context, cancellationToken);

        protected virtual bool OnCanExecute(ActivityExecutionContext context) => OnCanExecute();
        protected virtual bool OnCanExecute() => true;

        protected virtual ValueTask<bool> OnCanExecuteAsync(
            ActivityExecutionContext context,
            CancellationToken cancellationToken) => OnCanExecuteAsync(cancellationToken);

        protected virtual ValueTask<bool> OnCanExecuteAsync(CancellationToken cancellationToken) =>
            new ValueTask<bool>(OnCanExecute());

        protected virtual ValueTask<IActivityExecutionResult> OnExecuteAsync(
            ActivityExecutionContext context,
            CancellationToken cancellationToken) => new ValueTask<IActivityExecutionResult>(OnExecute(context));

        protected virtual ValueTask<IActivityExecutionResult> OnResumeAsync(
            ActivityExecutionContext context,
            CancellationToken cancellationToken) => new ValueTask<IActivityExecutionResult>(OnResume(context));

        protected virtual IActivityExecutionResult OnExecute(ActivityExecutionContext context) => OnExecute();
        protected virtual IActivityExecutionResult OnExecute() => Done();
        protected virtual IActivityExecutionResult OnResume(ActivityExecutionContext context) => OnResume();
        protected virtual IActivityExecutionResult OnResume() => Done();
        protected NoopResult Noop() => new NoopResult();
        protected OutcomeResult Done() => Outcome(OutcomeNames.Done);
        protected CombinedResult Done(object output) => Combine(Output(output), Done());
        protected OutcomeResult Outcomes(IEnumerable<string> outcomes) => new OutcomeResult(outcomes);
        protected OutcomeResult Outcomes(params string[] outcomes) => Outcomes((IEnumerable<string>)outcomes);
        protected OutcomeResult Outcome(string outcome) => Outcomes(outcome);
        protected CombinedResult Outcome(string outcome, object output) => Combine(Output(output), Outcome(outcome));
        protected OutputResult Output(object output) => new OutputResult(output);

        protected SuspendResult Suspend() => new SuspendResult();

        protected ScheduleActivitiesResult Schedule(params string[] activityIds) =>
            new ScheduleActivitiesResult(activityIds);

        protected ScheduleActivitiesResult Schedule(IEnumerable<string> activityIds, object input) =>
            new ScheduleActivitiesResult(activityIds, input);

        protected ScheduleActivitiesResult Schedule(string activityId, object input) =>
            Schedule(new[] { activityId }, input);

        protected ScheduleActivitiesResult Schedule(IEnumerable<ScheduledActivity> activities) =>
            new ScheduleActivitiesResult(activities);

        protected CombinedResult Combine(IEnumerable<IActivityExecutionResult> results) => new CombinedResult(results);
        protected CombinedResult Combine(params IActivityExecutionResult[] results) => new CombinedResult(results);
        protected FaultResult Fault(LocalizedString message) => new FaultResult(message);
        
        protected T GetState<T>(Func<T>? defaultValue = null, [CallerMemberName] string name = null!) => Data.GetState(name, defaultValue);
        protected T GetState<T>(Type type, Func<T>? defaultValue = null, [CallerMemberName] string name = null!) => Data.GetState(type, name, defaultValue);
        protected void SetState(object? value, [CallerMemberName] string name = null!) => Data.SetState(name, value);
    }
}