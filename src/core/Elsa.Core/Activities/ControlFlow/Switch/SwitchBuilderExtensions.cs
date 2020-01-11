using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class SwitchBuilderExtensions
    {
        public static IWorkflowBuilder Switch(this IBuilder builder, IWorkflowExpression<string> value, Action<SwitchBuilder> setup) => builder.Switch(x => x.WithValue(value), setup);
        public static IWorkflowBuilder Switch(this IBuilder builder, Func<ActivityExecutionContext, string> value, Action<SwitchBuilder> setup) => builder.Switch(x => x.WithValue(value), setup);
        public static IWorkflowBuilder Switch(this IBuilder builder, Func<string> value, Action<SwitchBuilder> setup) => builder.Switch(x => x.WithValue(value), setup);
        public static IWorkflowBuilder Switch(this IBuilder builder, string value, Action<SwitchBuilder> setup) => builder.Switch(x => x.WithValue(value), setup);

        private static IWorkflowBuilder Switch(this IBuilder builder, Action<Switch> setup, Action<SwitchBuilder> build)
        {
            var activityBuilder = builder.Then(setup);
            var switchBuilder = new SwitchBuilder(activityBuilder);
            
            build(switchBuilder);
            return activityBuilder.WorkflowBuilder;
        }
    }
}