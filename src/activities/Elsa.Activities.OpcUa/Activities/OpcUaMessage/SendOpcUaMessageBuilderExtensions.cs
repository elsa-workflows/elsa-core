using Elsa.Builders;
using Elsa.Services.Models;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Elsa.Activities.OpcUa
{
    public static class SendOpcUaMessageBuilderExtensions
    {
        public static IActivityBuilder SendMessage(this IBuilder builder, Action<ISetupActivity<SendOpcUaMessage>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder SendMessage(this IBuilder builder, string connectionString, Dictionary<string, string> tags, Func<ActivityExecutionContext, ValueTask<string>> message, [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.SendMessage(setup => setup.WithConnectionString(connectionString).WithTags(tags).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendMessage(this IBuilder builder, string connectionString, Dictionary<string, string> tags, string message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendMessage(setup => setup.WithConnectionString(connectionString).WithTags(tags).WithMessage(message), lineNumber, sourceFile);
    }
        
}
