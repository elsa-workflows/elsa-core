using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Services.Models;
using Microsoft.Extensions.Localization;
using ScheduledActivity = Elsa.Services.Models.ScheduledActivity;

namespace Elsa.Services
{
    public abstract class Activity : IActivity
    {
        public object? Output { get; set; }
        public virtual string Type => GetType().Name;
        public string Id { get; set; } = default!;
        public string? Name { get; set; }
        public string? DisplayName{ get; set; }
        public string? Description{ get; set; }
        public bool PersistWorkflow { get; set; }

        public ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken) => OnCanExecuteAsync(context, cancellationToken);
        public ValueTask<IActivityExecutionResult> ExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken) => OnExecuteAsync(context, cancellationToken);

        public ValueTask<IActivityExecutionResult> ResumeAsync(ActivityExecutionContext context, CancellationToken cancellationToken) => OnResumeAsync(context, cancellationToken);
        protected virtual bool OnCanExecute(ActivityExecutionContext context) => OnCanExecute();
        protected virtual bool OnCanExecute() => true;
        protected virtual ValueTask<bool> OnCanExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken) => OnCanExecuteAsync(cancellationToken);
        protected virtual ValueTask<bool> OnCanExecuteAsync(CancellationToken cancellationToken) => new ValueTask<bool>(OnCanExecute());
        protected virtual ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken) => new ValueTask<IActivityExecutionResult>(OnExecute(context));
        protected virtual ValueTask<IActivityExecutionResult> OnResumeAsync(ActivityExecutionContext context, CancellationToken cancellationToken) => new ValueTask<IActivityExecutionResult>(OnResume(context));
        protected virtual IActivityExecutionResult OnExecute(ActivityExecutionContext context) => OnExecute();
        protected virtual IActivityExecutionResult OnExecute() => Done();
        protected virtual IActivityExecutionResult OnResume(ActivityExecutionContext context) => OnResume();
        protected virtual IActivityExecutionResult OnResume() => Done();
        protected NoopResult Noop() => new NoopResult();
        protected OutcomeResult Done() => new OutcomeResult();
        protected OutcomeResult Done(string outcome, object? output) => Done(new[] { outcome }, output);
        protected OutcomeResult Done(IEnumerable<string> outcomes, object? output) => new OutcomeResult(outcomes, output);
        protected OutcomeResult Done(IEnumerable<string> outcomes) => Done(outcomes, default);
        protected OutcomeResult Done(params string[] outcomes) => Done(outcomes, default);
        protected OutcomeResult Done(object? output) => new OutcomeResult(null, output);
        protected SuspendResult Suspend() => new SuspendResult();
        protected ScheduleActivitiesResult Schedule(params IActivity[] activities) => new ScheduleActivitiesResult(activities);
        protected ScheduleActivitiesResult Schedule(IEnumerable<IActivity> activities, object input) => new ScheduleActivitiesResult(activities, input);
        protected ScheduleActivitiesResult Schedule(IActivity activity, object input) => Schedule(new[] { activity }, input);
        protected ScheduleActivitiesResult Schedule(IEnumerable<ScheduledActivity> activities) => new ScheduleActivitiesResult(activities);
        protected CombinedResult Combine(IEnumerable<IActivityExecutionResult> results) => new CombinedResult(results);
        protected CombinedResult Combine(params IActivityExecutionResult[] results) => new CombinedResult(results);
        protected FaultResult Fault(LocalizedString message) => new FaultResult(message);
    }
}