using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public static class ActivityBuilderExtensions
    {
        public static ActivityBuilder Then(this IBuilder builder, Func<ActivityExecutionContext, Task<IActivityExecutionResult>> activity) => builder.Then(new Inline(activity));
        public static ActivityBuilder Then(this IBuilder builder, Func<ActivityExecutionContext, Task> activity) => builder.Then(new Inline(activity));
        public static ActivityBuilder Then(this IBuilder builder, Action<ActivityExecutionContext> activity) => builder.Then(new Inline(activity));
        public static ActivityBuilder Then(this IBuilder builder, Action activity) => builder.Then(new Inline(activity));

        public static IfElseBuilder If(this ActivityBuilder activityBuilder, IWorkflowExpression<bool> condition)
        {
            var ifElse = activityBuilder.Then<IfElse>(x => x.Condition = condition);
            return new IfElseBuilder(ifElse);
        }

        public static IfElseBuilder If(this ActivityBuilder activityBuilder, Func<bool> condition) => If(activityBuilder, new CodeExpression<bool>(condition));
        public static IfElseBuilder If(this ActivityBuilder activityBuilder, Func<ActivityExecutionContext, bool> condition) => If(activityBuilder, new CodeExpression<bool>(condition));
    }
}