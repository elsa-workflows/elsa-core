using Elsa.Builders;
using Elsa.Services.Models;
using System;
using System.Net.Mqtt;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Elsa.Activities.Mqtt
{
    public static class SendMqttMessageBuilderExtensions
    {
        public static IActivityBuilder SendMessage(this IBuilder builder, Action<ISetupActivity<SendMqttMessage>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder SendMessage(this IBuilder builder, string topic, string host, int port, string username, string password, MqttQualityOfService qos, Func<ActivityExecutionContext, ValueTask<string>> message, [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.SendMessage(setup => setup.WithTopic(topic).WithHost(host).WithPort(port).WithUsername(username).WithPassword(password).WithQualityOfService(qos).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendMessage(this IBuilder builder, string topic, string host, int port, string username, string password, MqttQualityOfService qos, Func<ActivityExecutionContext, string> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendMessage(setup => setup.WithTopic(topic).WithHost(host).WithPort(port).WithUsername(username).WithPassword(password).WithQualityOfService(qos).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendMessage(this IBuilder builder, string topic, string host, int port, string username, string password, MqttQualityOfService qos, Func<string> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendMessage(setup => setup.WithTopic(topic).WithHost(host).WithPort(port).WithUsername(username).WithPassword(password).WithQualityOfService(qos).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendMessage(this IBuilder builder, string topic, string host, int port, string username, string password, MqttQualityOfService qos, string message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendMessage(setup => setup.WithTopic(topic).WithHost(host).WithPort(port).WithUsername(username).WithPassword(password).WithQualityOfService(qos).WithMessage(message), lineNumber, sourceFile);
    }
}
