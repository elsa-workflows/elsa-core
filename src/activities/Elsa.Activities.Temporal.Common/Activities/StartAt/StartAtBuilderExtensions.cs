using System;
using System.Runtime.CompilerServices;
using Elsa.Builders;
using Elsa.Services.Models;
using NodaTime;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Temporal
{
    public static class StartAtBuilderExtensions
    {
        public static IActivityBuilder StartAt(this IBuilder builder, Action<ISetupActivity<StartAt>>? setup = default, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder StartAt(this IBuilder builder, Func<ActivityExecutionContext, Instant> instant, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.StartAt(setup => setup.Set(x => x.Instant, instant), lineNumber, sourceFile);

        public static IActivityBuilder StartAt(this IBuilder builder, Func<Instant> instant, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.StartAt(setup => setup.Set(x => x.Instant, instant), lineNumber, sourceFile);

        public static IActivityBuilder StartAt(this IBuilder builder, Instant instant, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.StartAt(setup => setup.Set(x => x.Instant, instant), lineNumber, sourceFile);

        public static IActivityBuilder StartIn(this IBuilder builder, Func<ActivityExecutionContext, Duration> timeout, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.StartAt(context => Now(context).Plus(timeout(context)), lineNumber, sourceFile);

        public static IActivityBuilder StartIn(this IBuilder builder, Func<Duration> timeout, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.StartAt(context => Now(context).Plus(timeout()), lineNumber, sourceFile);

        public static IActivityBuilder StartIn(this IBuilder builder, Duration timeout, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.StartAt(context => Now(context).Plus(timeout), lineNumber, sourceFile);

        private static Instant Now(ActivityExecutionContext context) => context.GetService<IClock>().GetCurrentInstant();
    }
}