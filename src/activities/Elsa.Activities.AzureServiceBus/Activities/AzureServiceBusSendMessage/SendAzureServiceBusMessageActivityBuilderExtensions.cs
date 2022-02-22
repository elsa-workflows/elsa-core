using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable ExplicitCallerInfoArgument
namespace Elsa.Activities.AzureServiceBus
{
    public static class SendAzureServiceBusMessageActivityBuilderExtensions
    {
        public static IActivityBuilder SendMessage(this IBuilder builder, Action<ISetupActivity<SendAzureServiceBusMessageActivity>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) => builder.Then(setup, null, lineNumber, sourceFile);
        
        public static IActivityBuilder SendQueueMessage(this IBuilder builder, string queue, Func<ActivityExecutionContext, ValueTask<object>> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) => builder.SendMessage(setup => setup.WithQueue(queue).WithMessage(message), lineNumber, sourceFile);
        public static IActivityBuilder SendQueueMessage(this IBuilder builder, string queue, Func<ActivityExecutionContext, object> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) => builder.SendMessage(setup => setup.WithQueue(queue).WithMessage(message), lineNumber, sourceFile);
        public static IActivityBuilder SendQueueMessage(this IBuilder builder, string queue, Func<object> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) => builder.SendMessage(setup => setup.WithQueue(queue).WithMessage(message), lineNumber, sourceFile);
        public static IActivityBuilder SendQueueMessage(this IBuilder builder, string queue, object message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) => builder.SendMessage(setup => setup.WithQueue(queue).WithMessage(message), lineNumber, sourceFile);
        
        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string topic, Func<ActivityExecutionContext, ValueTask<object>> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) => builder.SendMessage(setup => setup.WithTopic(topic).WithMessage(message), lineNumber, sourceFile);
        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string topic, Func<ActivityExecutionContext, object> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) => builder.SendMessage(setup => setup.WithTopic(topic).WithMessage(message), lineNumber, sourceFile);
        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string topic, Func<object> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) => builder.SendMessage(setup => setup.WithTopic(topic).WithMessage(message), lineNumber, sourceFile);
        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string topic, object message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) => builder.SendMessage(setup => setup.WithTopic(topic).WithMessage(message), lineNumber, sourceFile);
    }
}