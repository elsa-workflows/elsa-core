using Elsa.Builders;
using Elsa.Services.Models;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Elsa.Activities.RabbitMq
{
    public static class SendRabbitMqMessageBuilderExtensions
    {
        public static IActivityBuilder SendQueueMessage(this IBuilder builder, Action<ISetupActivity<SendRabbitMqMessage>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder SendQueueMessage(this IBuilder builder, string connectionString, string topic, Dictionary<string, string> headers, Func<ActivityExecutionContext, ValueTask<string>> message, [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.SendQueueMessage(setup => setup.WithConnectionString(connectionString).WithTopic(topic).WithHeaders(headers).WithMessage(message), lineNumber, sourceFile);
    }
}
