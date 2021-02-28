using System;
using System.Runtime.CompilerServices;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Temporal
{
    public static class ClearTimerExtensions
    {
        public static IActivityBuilder CancelTimer(this IBuilder builder, Action<ISetupActivity<ClearTimer>>? setup = default, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder CancelTimer(this IBuilder builder, Func<ActivityExecutionContext, string> activityId, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.CancelTimer(setup => setup.Set(x => x.ActivityId, activityId), lineNumber, sourceFile);

        public static IActivityBuilder CancelTimer(this IBuilder builder, Func<string> activityId, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.CancelTimer(setup => setup.Set(x => x.ActivityId, activityId), lineNumber, sourceFile);

        public static IActivityBuilder CancelTimer(this IBuilder builder, string activityId, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.CancelTimer(setup => setup.Set(x => x.ActivityId, activityId), lineNumber, sourceFile);
    }
}