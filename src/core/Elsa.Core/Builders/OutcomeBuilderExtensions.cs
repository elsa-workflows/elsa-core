using System;
using System.Threading.Tasks;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public static class OutcomeBuilderExtensions
    {
        public static ActivityBuilder Then(this OutcomeBuilder outcomeBuilder, Func<WorkflowExecutionContext, ActivityExecutionContext, Task<IActivityExecutionResult>> activity, Action<ActivityBuilder> branch = default) => outcomeBuilder.Then(new Inline(activity), branch);
        public static ActivityBuilder Then(this OutcomeBuilder outcomeBuilder, Func<WorkflowExecutionContext, ActivityExecutionContext, Task> activity, Action<ActivityBuilder> branch = default) => outcomeBuilder.Then(new Inline(activity), branch);
        public static ActivityBuilder Then(this OutcomeBuilder outcomeBuilder, Action<WorkflowExecutionContext, ActivityExecutionContext> activity, Action<ActivityBuilder> branch = default) => outcomeBuilder.Then(new Inline(activity), branch);
        public static ActivityBuilder Then(this OutcomeBuilder outcomeBuilder, Action activity, Action<ActivityBuilder> branch = default) => outcomeBuilder.Then(new Inline(activity), branch);
    }
}