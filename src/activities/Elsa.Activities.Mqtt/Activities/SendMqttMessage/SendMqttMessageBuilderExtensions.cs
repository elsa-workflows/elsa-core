using System;
using MQTTnet.Protocol;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

namespace Elsa.Activities.Mqtt.Activities.SendMqttMessage
{
    public static class SendMqttMessageBuilderExtensions
    {
        public static IActivityBuilder SendMessage(this IBuilder builder, Action<ISetupActivity<SendMqttMessage>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder SendMessage(this IBuilder builder, string topic, string host, int port, string username, string password, MqttQualityOfServiceLevel qos, Func<ActivityExecutionContext, ValueTask<string>> message, [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.SendMessage(setup => setup.WithTopic(topic).WithHost(host).WithPort(port).WithUsername(username).WithPassword(password).WithQualityOfService(qos).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendMessage(this IBuilder builder, string topic, string host, int port, string username, string password, MqttQualityOfServiceLevel qos, Func<ActivityExecutionContext, string> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendMessage(setup => setup.WithTopic(topic).WithHost(host).WithPort(port).WithUsername(username).WithPassword(password).WithQualityOfService(qos).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendMessage(this IBuilder builder, string topic, string host, int port, string username, string password, MqttQualityOfServiceLevel qos, Func<string> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendMessage(setup => setup.WithTopic(topic).WithHost(host).WithPort(port).WithUsername(username).WithPassword(password).WithQualityOfService(qos).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendMessage(this IBuilder builder, string topic, string host, int port, string username, string password, MqttQualityOfServiceLevel qos, string message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendMessage(setup => setup.WithTopic(topic).WithHost(host).WithPort(port).WithUsername(username).WithPassword(password).WithQualityOfService(qos).WithMessage(message), lineNumber, sourceFile);
    }
}
