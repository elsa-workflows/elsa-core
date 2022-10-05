using System;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Services.Models;

namespace Elsa.Builders;

public class RunInlineHelpers
{
    public static Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> RunInline(
        Func<ActivityExecutionContext, ValueTask> activity) =>
        async context =>
        {
            await activity(context);
            return new DoneResult();
        };

    public static Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> RunInline(
        Action<ActivityExecutionContext> activity) =>
        context =>
        {
            activity(context);
            return new ValueTask<IActivityExecutionResult>(new DoneResult());
        };

    public static Func<ActivityExecutionContext, ValueTask<IActivityExecutionResult>> RunInline(Action activity) =>
        _ =>
        {
            activity();
            return new ValueTask<IActivityExecutionResult>(new DoneResult());
        };
}