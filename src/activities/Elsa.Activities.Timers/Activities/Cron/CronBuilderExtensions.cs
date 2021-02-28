using System;
using System.Runtime.CompilerServices;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Temporal
{
    public static class CronBuilderExtensions
    {
        public static IActivityBuilder Cron(this IBuilder builder, Action<ISetupActivity<Cron>>? setup = default, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder Cron(this IBuilder builder, Func<ActivityExecutionContext, string> cronExpression, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Cron(setup => setup.Set(x => x.CronExpression, cronExpression), lineNumber, sourceFile);

        public static IActivityBuilder Cron(this IBuilder builder, Func<string> cronExpression, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Cron(setup => setup.Set(x => x.CronExpression, cronExpression), lineNumber, sourceFile);

        public static IActivityBuilder Cron(this IBuilder builder, string cronExpression, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Cron(setup => setup.Set(x => x.CronExpression, cronExpression), lineNumber, sourceFile);
    }
}