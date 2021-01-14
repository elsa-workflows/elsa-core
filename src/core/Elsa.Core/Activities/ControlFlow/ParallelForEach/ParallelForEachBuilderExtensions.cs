using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class ParallelForEachBuilderExtensions
    {
        public static IActivityBuilder ParallelForEach(
            this IBuilder builder,
            Action<ISetupActivity<ParallelForEach>> setup,
            Action<IOutcomeBuilder> iterate,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, branch => iterate(branch.When(OutcomeNames.Iterate)), lineNumber, sourceFile);

        public static IActivityBuilder ParallelForEach(
            this IBuilder builder,
            Func<ActivityExecutionContext, ICollection<object>> items,
            Action<IOutcomeBuilder> iterate,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.ParallelForEach(activity => activity.Set(x => x.Items, items), iterate, lineNumber, sourceFile);

        public static IActivityBuilder ParallelForEach(
            this IBuilder builder,
            Func<ICollection<object>> items,
            Action<IOutcomeBuilder> iterate,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.ParallelForEach(activity => activity.Set(x => x.Items, items), iterate, lineNumber, sourceFile);

        public static IActivityBuilder ParallelForEach(
            this IBuilder builder,
            ICollection<object> items,
            Action<IOutcomeBuilder> iterate,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.ParallelForEach(activity => activity.Set(x => x.Items, items), iterate, lineNumber, sourceFile);
    }
}