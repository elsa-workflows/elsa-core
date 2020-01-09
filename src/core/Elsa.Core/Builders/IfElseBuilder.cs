using System;
using Elsa.Services;

namespace Elsa.Builders
{
    public class IfElseBuilder
    {
        public IfElseBuilder(ActivityBuilder activityBuilder)
        {
            ActivityBuilder = activityBuilder;
        }

        public ActivityBuilder ActivityBuilder { get; }

        public IfElseBuilder Then<T>(T activity, Action<ActivityBuilder> activityBuilder) where T : class, IActivity => When(activity, OutcomeNames.True, activityBuilder);
        public IfElseBuilder Then<T>(Action<T> setup, Action<ActivityBuilder> activityBuilder) where T : class, IActivity => When(setup, OutcomeNames.True, activityBuilder);
        public IfElseBuilder Else<T>(T activity, Action<ActivityBuilder> activityBuilder) where T : class, IActivity => When(activity, OutcomeNames.False, activityBuilder);
        public IfElseBuilder Else<T>(Action<T> setup, Action<ActivityBuilder> activityBuilder) where T : class, IActivity => When(setup, OutcomeNames.False, activityBuilder);

        private IfElseBuilder When<T>(T activity, string outcome, Action<ActivityBuilder> activityBuilder) where T : class, IActivity
        {
            activityBuilder(ActivityBuilder.When(outcome).Then(activity));

            return this;
        }

        private IfElseBuilder When<T>(Action<T> activity, string outcome, Action<ActivityBuilder> activityBuilder) where T : class, IActivity
        {
            activityBuilder(ActivityBuilder.When(outcome).Then(activity));

            return this;
        }
    }
}