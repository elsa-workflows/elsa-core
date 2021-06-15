using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public abstract class Activity : IActivity
    {
        public virtual string Type => GetType().Name;
        public virtual string Id { get; set; } = default!;
        public virtual string? Name { get; set; }
        public virtual string? DisplayName { get; set; }
        public virtual string? Description { get; set; }
        public virtual bool PersistWorkflow { get; set; }
        public virtual bool LoadWorkflowContext { get; set; }
        public virtual bool SaveWorkflowContext { get; set; }
        public virtual IDictionary<string, object?> Data { get; set; } = new Dictionary<string, object?>();
        public virtual ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context) => OnCanExecuteAsync(context);
        public virtual ValueTask<IActivityExecutionResult> ExecuteAsync(ActivityExecutionContext context) => OnExecuteAsync(context);
        public virtual ValueTask<IActivityExecutionResult> ResumeAsync(ActivityExecutionContext context) => OnResumeAsync(context);
        protected virtual ValueTask<bool> OnCanExecuteAsync(ActivityExecutionContext context) => new(OnCanExecute(context));
        protected virtual ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context) => new(OnExecute(context));
        protected virtual ValueTask<IActivityExecutionResult> OnResumeAsync(ActivityExecutionContext context) => new(OnResume(context));
        protected virtual bool OnCanExecute(ActivityExecutionContext context) => true;
        protected virtual IActivityExecutionResult OnExecute(ActivityExecutionContext context) => OnExecute();
        protected virtual IActivityExecutionResult OnExecute() => Done();
        protected virtual IActivityExecutionResult OnResume(ActivityExecutionContext context) => OnResume();
        protected virtual IActivityExecutionResult OnResume() => Done();
        protected virtual NoopResult Noop() => new();
        protected virtual OutcomeResult Done() => Outcome(OutcomeNames.Done);
        protected virtual OutcomeResult Done(object? input) => Outcome(OutcomeNames.Done, input);
        protected virtual OutcomeResult Outcomes(IEnumerable<string> outcomes, object? input = null) => new(outcomes, input);
        protected virtual OutcomeResult Outcomes(params string[] outcomes) => Outcomes((IEnumerable<string>) outcomes);
        protected virtual OutcomeResult Outcomes(object? input, params string[] outcomes) => Outcomes((IEnumerable<string>) outcomes, input);
        protected virtual OutcomeResult Outcome(string outcome, object? input = null) => Outcomes(input, outcome);

        protected virtual SuspendResult Suspend() => new();
        protected virtual ScheduleActivitiesResult Schedule(params string[] activityIds) => new(activityIds);
        protected virtual ScheduleActivitiesResult Schedule(IEnumerable<string> activityIds) => new(activityIds);
        protected virtual ScheduleActivitiesResult Schedule(string activityId) => Schedule(new[] { activityId });
        protected virtual ScheduleActivitiesResult Schedule(IEnumerable<ScheduledActivity> activities) => new(activities);
        protected virtual CombinedResult Combine(IEnumerable<IActivityExecutionResult> results) => new(results);
        protected virtual CombinedResult Combine(params IActivityExecutionResult[] results) => new(results);
        protected virtual FaultResult Fault(Exception exception) => new(exception);
        protected virtual FaultResult Fault(string message) => new(message);
        protected virtual RegisterTaskResult RegisterTask(Func<WorkflowExecutionContext, CancellationToken, ValueTask> task) => new(task);

        protected virtual T? GetState<T>([CallerMemberName] string name = null!) => Data.GetState<T>(name);
        protected virtual T GetState<T>(Func<T> defaultValue, [CallerMemberName] string name = null!) => Data.GetState(name, defaultValue);
        protected virtual void SetState(object? value, [CallerMemberName] string name = null!) => Data.SetState(name, value);
    }
}