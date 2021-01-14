using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        public bool LoadWorkflowContext { get; set; }
        public bool SaveWorkflowContext { get; set; }
        public JObject Data { get; set; } = new();
        public ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context) => OnCanExecuteAsync(context);
        public ValueTask<IActivityExecutionResult> ExecuteAsync(ActivityExecutionContext context) => OnExecuteAsync(context);
        public ValueTask<IActivityExecutionResult> ResumeAsync(ActivityExecutionContext context) => OnResumeAsync(context);
        protected virtual ValueTask<bool> OnCanExecuteAsync(ActivityExecutionContext context) => new(OnCanExecute(context));
        protected virtual ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context) => new(OnExecute(context));
        protected virtual ValueTask<IActivityExecutionResult> OnResumeAsync(ActivityExecutionContext context) => new(OnResume(context));
        protected virtual bool OnCanExecute(ActivityExecutionContext context) => true;
        protected virtual IActivityExecutionResult OnExecute(ActivityExecutionContext context) => OnExecute();
        protected virtual IActivityExecutionResult OnExecute() => Done();
        protected virtual IActivityExecutionResult OnResume(ActivityExecutionContext context) => OnResume();
        protected virtual IActivityExecutionResult OnResume() => Done();
        protected NoopResult Noop() => new();
        protected OutcomeResult Done() => Outcome(OutcomeNames.Done);
        protected CombinedResult Done(object? output) => Combine(Output(output), Done());
        protected OutcomeResult Outcomes(IEnumerable<string> outcomes) => new(outcomes);
        protected OutcomeResult Outcomes(params string[] outcomes) => Outcomes((IEnumerable<string>)outcomes);
        protected OutcomeResult Outcome(string outcome) => Outcomes(outcome);
        protected CombinedResult Outcome(string outcome, object? output) => Combine(Output(output), Outcome(outcome));
        protected OutputResult Output(object? output) => new(output);
        protected SuspendResult Suspend() => new();
        protected ScheduleActivitiesResult Schedule(params string[] activityIds) => new(activityIds);
        protected ScheduleActivitiesResult Schedule(IEnumerable<string> activityIds, object? input) => new(activityIds, input);
        protected ScheduleActivitiesResult Schedule(string activityId, object? input) => Schedule(new[] { activityId }, input);
        protected ScheduleActivitiesResult Schedule(IEnumerable<ScheduledActivity> activities) => new(activities);
        protected CombinedResult Combine(IEnumerable<IActivityExecutionResult> results) => new(results);
        protected CombinedResult Combine(params IActivityExecutionResult[] results) => new(results);
        protected FaultResult Fault(string message) => new(message);
        
        protected T? GetState<T>([CallerMemberName] string name = null!) => Data.GetState<T>(name);
        protected T GetState<T>(Func<T> defaultValue, [CallerMemberName] string name = null!) => Data.GetState(name, defaultValue);
        protected T GetState<T>(Type type, Func<T> defaultValue, [CallerMemberName] string name = null!) => Data.GetState(type, name, defaultValue);
        protected void SetState(object? value, [CallerMemberName] string name = null!) => Data.SetState(name, value);
    }
}