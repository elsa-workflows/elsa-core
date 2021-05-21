using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Signaling
{
    public static class SendSignalBuilderExtensions
    {
        public static IActivityBuilder SendSignal(this IBuilder builder, Action<ISetupActivity<SendSignal>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder SendSignal(this IBuilder builder, Func<ActivityExecutionContext, string> signal, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendSignal(activity => activity.WithSignal(signal), lineNumber, sourceFile);

        public static IActivityBuilder SendSignal(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<string>> signal, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendSignal(activity => activity.WithSignal(signal!), lineNumber, sourceFile);

        public static IActivityBuilder SendSignal(this IBuilder builder, Func<string> signal, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendSignal(activity => activity.WithSignal(signal!), lineNumber, sourceFile);

        public static IActivityBuilder SendSignal(this IBuilder builder, Func<ValueTask<string>> signal, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendSignal(activity => activity.WithSignal(signal!), lineNumber, sourceFile);

        public static IActivityBuilder SendSignal(this IBuilder builder, string signal, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendSignal(activity => activity.WithSignal(signal!), lineNumber, sourceFile);
    }
}