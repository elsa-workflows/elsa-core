using System;
using System.Threading.Tasks;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http.Features.Authentication;

namespace Elsa.Builders
{
    public static class IfElseBuilderExtensions
    {
        public static IfElseBuilder Then(this IfElseBuilder ifElseBuilder, Func<WorkflowExecutionContext, ActivityExecutionContext, Task<IActivityExecutionResult>> activity, Action<ActivityBuilder> activityBuilder) => ifElseBuilder.Then(new Inline(activity), activityBuilder);
        public static IfElseBuilder Then(this IfElseBuilder ifElseBuilder, Func<WorkflowExecutionContext, ActivityExecutionContext, Task> activity, Action<ActivityBuilder> activityBuilder) => ifElseBuilder.Then(new Inline(activity), activityBuilder);
        public static IfElseBuilder Then(this IfElseBuilder ifElseBuilder, Action<WorkflowExecutionContext, ActivityExecutionContext> activity, Action<ActivityBuilder> activityBuilder) => ifElseBuilder.Then(new Inline(activity), activityBuilder);
        public static IfElseBuilder Then(this IfElseBuilder ifElseBuilder, Action activity, Action<ActivityBuilder> activityBuilder) => ifElseBuilder.Then(new Inline(activity), activityBuilder);
        public static IfElseBuilder Else(this IfElseBuilder ifElseBuilder, Func<WorkflowExecutionContext, ActivityExecutionContext, Task<IActivityExecutionResult>> activity, Action<ActivityBuilder> activityBuilder) => ifElseBuilder.Else(new Inline(activity), activityBuilder);
        public static IfElseBuilder Else(this IfElseBuilder ifElseBuilder, Func<WorkflowExecutionContext, ActivityExecutionContext, Task> activity, Action<ActivityBuilder> activityBuilder) => ifElseBuilder.Else(new Inline(activity), activityBuilder);
        public static IfElseBuilder Else(this IfElseBuilder ifElseBuilder, Action<WorkflowExecutionContext, ActivityExecutionContext> activity, Action<ActivityBuilder> activityBuilder) => ifElseBuilder.Else(new Inline(activity), activityBuilder);
        public static IfElseBuilder Else(this IfElseBuilder ifElseBuilder, Action activity, Action<ActivityBuilder> activityBuilder) => ifElseBuilder.Else(new Inline(activity), activityBuilder);
    }
}