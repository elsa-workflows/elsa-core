using System;
using System.Threading.Tasks;
using Elsa.Activities.Primitives;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public class ForEachBuilder
    {
        public ForEachBuilder(ActivityBuilder activityBuilder)
        {
            ActivityBuilder = activityBuilder;
        }
        
        public ActivityBuilder ActivityBuilder { get; }

        public ForEachBuilder Iterate<T>(T activity, Action<ActivityBuilder>? activityBuilder = null) where T : class, IActivity
        {
            var builder = ActivityBuilder.When(OutcomeNames.Iterate).Then(activity);
            activityBuilder?.Invoke(builder);
            return this;
        }

        public ForEachBuilder Iterate<T>(Action<T> setup, Action<ActivityBuilder>? activityBuilder = null) where T : class, IActivity
        {
            var builder = ActivityBuilder.When(OutcomeNames.Iterate).Then(setup);
            activityBuilder?.Invoke(builder);
            return this;
        }

        public ForEachBuilder Iterate(Action activity) => Iterate(new Inline(activity));
        public ForEachBuilder Iterate(Action<WorkflowExecutionContext, ActivityExecutionContext> activity) => Iterate(new Inline(activity));
        public ForEachBuilder Iterate(Func<WorkflowExecutionContext, ActivityExecutionContext, Task> activity) => Iterate(new Inline(activity));
        public ForEachBuilder Iterate(Func<WorkflowExecutionContext, ActivityExecutionContext, Task<IActivityExecutionResult>> activity) => Iterate(new Inline(activity));
        public ActivityBuilder Then<T>(T activity, Action<ActivityBuilder>? activityBuilder = null) where T : class, IActivity
        {
            var builder = ActivityBuilder.When(OutcomeNames.Done).Then(activity);
            activityBuilder?.Invoke(builder);
            return builder;
        }

        public ActivityBuilder Then<T>(Action<T> setup, Action<ActivityBuilder>? activityBuilder = null) where T : class, IActivity
        {
            var builder = ActivityBuilder.When(OutcomeNames.Done).Then(setup);
            activityBuilder?.Invoke(builder);
            return builder;
        }

        public ActivityBuilder Then(Action activity) => Then(new Inline(activity));
        public ActivityBuilder Then(Action<WorkflowExecutionContext, ActivityExecutionContext> activity) => Then(new Inline(activity));
        public ActivityBuilder Then(Func<WorkflowExecutionContext, ActivityExecutionContext, Task> activity) => Then(new Inline(activity));
        public ActivityBuilder Then(Func<WorkflowExecutionContext, ActivityExecutionContext, Task<IActivityExecutionResult>> activity) => Then(new Inline(activity));
    }
}