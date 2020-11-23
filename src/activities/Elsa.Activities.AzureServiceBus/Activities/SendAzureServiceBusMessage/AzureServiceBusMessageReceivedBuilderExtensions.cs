using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

namespace Elsa.Activities.AzureServiceBus
{
    public static class SendAzureServiceBusMessageBuilderExtensions
    {
        public static IActivityBuilder SendMessage(this IBuilder builder, Action<ISetupActivity<SendAzureServiceBusMessage>> setup) => builder.Then(setup);
        public static IActivityBuilder SendMessage(this IBuilder builder, string queueName, Func<ActivityExecutionContext, ValueTask<object>> message) => builder.SendMessage(setup => setup.WithQueueName(queueName).WithMessage(message));
        public static IActivityBuilder SendMessage(this IBuilder builder, string queueName, Func<ActivityExecutionContext, object> message) => builder.SendMessage(setup => setup.WithQueueName(queueName).WithMessage(message));
        public static IActivityBuilder SendMessage(this IBuilder builder, string queueName, Func<object> message) => builder.SendMessage(setup => setup.WithQueueName(queueName).WithMessage(message));
        public static IActivityBuilder SendMessage(this IBuilder builder, string queueName, object message) => builder.SendMessage(setup => setup.WithQueueName(queueName).WithMessage(message));
    }
}