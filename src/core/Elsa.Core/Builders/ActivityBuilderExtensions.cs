using System;
using System.Threading.Tasks;
using Elsa.Activities.Primitives;
using Elsa.Results;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public static class ActivityBuilderExtensions
    {
        public static IActivityBuilder Then(this IBuilder builder, Func<ActivityExecutionContext, Task<IActivityExecutionResult>> activity) => builder.Then(new Inline(activity));
        public static IActivityBuilder Then(this IBuilder builder, Func<ActivityExecutionContext, Task> activity) => builder.Then(new Inline(activity));
        public static IActivityBuilder Then(this IBuilder builder, Action<ActivityExecutionContext> activity) => builder.Then(new Inline(activity));
        public static IActivityBuilder Then(this IBuilder builder, Action activity) => builder.Then(new Inline(activity));
    }
}