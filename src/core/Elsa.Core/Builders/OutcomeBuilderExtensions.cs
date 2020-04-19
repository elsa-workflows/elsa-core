using System;
using System.Threading.Tasks;
using Elsa.Activities.Primitives;
using Elsa.Results;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public static class OutcomeBuilderExtensions
    {
        public static IActivityBuilder Then(this IOutcomeBuilder outcomeBuilder, Func<ActivityExecutionContext, Task<IActivityExecutionResult>> activity, Action<IActivityBuilder>? branch = default) => outcomeBuilder.Then(new Inline(activity), branch);
        public static IActivityBuilder Then(this IOutcomeBuilder outcomeBuilder, Func<ActivityExecutionContext, Task> activity, Action<IActivityBuilder>? branch = default) => outcomeBuilder.Then(new Inline(activity), branch);
        public static IActivityBuilder Then(this IOutcomeBuilder outcomeBuilder, Action<ActivityExecutionContext> activity, Action<IActivityBuilder>? branch = default) => outcomeBuilder.Then(new Inline(activity), branch);
        public static IActivityBuilder Then(this IOutcomeBuilder outcomeBuilder, Action activity, Action<IActivityBuilder>? branch = default) => outcomeBuilder.Then(new Inline(activity), branch);
    }
}