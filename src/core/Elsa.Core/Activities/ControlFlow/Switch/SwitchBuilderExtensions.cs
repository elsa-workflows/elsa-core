using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elsa.Builders;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class SwitchBuilderExtensions
    {
        public static IActivityBuilder Switch(
            this IBuilder builder,
            Func<SwitchCaseBuilder, ValueTask> cases,
            SwitchMode mode = SwitchMode.MatchFirst,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default)
        {
            var switchCaseBuilder = new SwitchCaseBuilder();

            var activityBuilder = builder.Then<Switch>(async setup =>
            {
                await cases(switchCaseBuilder);

                setup
                    .WithCases(async context =>
                    {
                        var evaluatedCases = switchCaseBuilder.Cases.Select(async x => new SwitchCase(x.Name, await x.Condition(context))).ToList();
                        return await Task.WhenAll(evaluatedCases);
                    })
                    .WithMode(mode);
            }, null, lineNumber, sourceFile);

            foreach (var caseDescriptor in switchCaseBuilder.Cases)
                caseDescriptor.OutcomeBuilder(activityBuilder);

            return activityBuilder;
        }

        public static IActivityBuilder Switch(
            this IBuilder builder,
            Action<SwitchCaseBuilder> cases,
            SwitchMode mode = SwitchMode.MatchFirst,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default)
        {
            return builder.Switch(caseBuilder =>
            {
                cases(caseBuilder);
                return new ValueTask();
            }, mode, lineNumber, sourceFile);
        }
    }
}