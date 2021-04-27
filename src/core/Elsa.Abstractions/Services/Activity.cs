using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Models;
using Elsa.Services.Models;
using Newtonsoft.Json.Linq;

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
        public virtual JObject Data { get; set; } = new();
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
        protected virtual CombinedResult Done(object? output) => Combine(Output(output), Done());
        protected virtual OutcomeResult Outcomes(IEnumerable<string> outcomes) => new(outcomes);
        protected virtual OutcomeResult Outcomes(params string[] outcomes) => Outcomes((IEnumerable<string>)outcomes);
        protected virtual OutcomeResult Outcome(string outcome) => Outcomes(outcome);
        protected virtual CombinedResult Outcome(string outcome, object? output) => Combine(Output(output), Outcome(outcome));
        protected virtual OutputResult Output(object? output) => new(output);
        protected virtual SuspendResult Suspend() => new();
        protected virtual ScheduleActivitiesResult Schedule(params string[] activityIds) => new(activityIds);
        protected virtual ScheduleActivitiesResult Schedule(IEnumerable<string> activityIds, object? input) => new(activityIds, input);
        protected virtual ScheduleActivitiesResult Schedule(string activityId, object? input) => Schedule(new[] { activityId }, input);
        protected virtual ScheduleActivitiesResult Schedule(IEnumerable<ScheduledActivity> activities) => new(activities);
        protected virtual CombinedResult Combine(IEnumerable<IActivityExecutionResult> results) => new(results);
        protected virtual CombinedResult Combine(params IActivityExecutionResult[] results) => new(results);
        protected virtual FaultResult Fault(Exception exception) => new(exception);
        protected virtual FaultResult Fault(string message) => new(message);
        protected virtual RegisterTaskResult RegisterTask(Func<WorkflowExecutionContext, CancellationToken, ValueTask> task) => new(task);
        
        protected virtual T? GetState<T>([CallerMemberName] string name = null!) => Data.GetState<T>(name);
        protected virtual T GetState<T>(Func<T> defaultValue, [CallerMemberName] string name = null!) => Data.GetState(name, defaultValue);
        protected virtual T GetState<T>(Type type, Func<T> defaultValue, [CallerMemberName] string name = null!) => Data.GetState(type, name, defaultValue);
        protected virtual void SetState(object? value, [CallerMemberName] string name = null!) => Data.SetState(name, value);
    }
}