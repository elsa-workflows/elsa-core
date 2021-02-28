using System;
using System.Runtime.CompilerServices;
using Elsa.Builders;
using Elsa.Services.Models;
using NodaTime;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Temporal
{
    public static class TimerBuilderExtensions
    {
        public static IActivityBuilder Timer(this IBuilder builder, Action<ISetupActivity<Timer>>? setup = default, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder
            Timer(this IBuilder builder, Func<ActivityExecutionContext, Duration> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Timer(setup => setup.Set(x => x.Timeout, value), lineNumber, sourceFile);

        public static IActivityBuilder Timer(this IBuilder builder, Func<Duration> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Timer(setup => setup.Set(x => x.Timeout, value), lineNumber, sourceFile);

        public static IActivityBuilder Timer(this IBuilder builder, Duration value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Timer(setup => setup.Set(x => x.Timeout, value), lineNumber, sourceFile);
    }
}