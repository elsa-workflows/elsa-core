using System;
using System.Runtime.CompilerServices;
using Elsa.Builders;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class ScheduleSendMassTransitMessageBuilderExtensions
    {
        public static IActivityBuilder ScheduleSendMassTransitMessage(
            this IBuilder builder, 
            Action<ISetupActivity<ScheduleSendMassTransitMessage>>? setup = default, 
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.Then(setup, null, lineNumber, sourceFile);
    }
}