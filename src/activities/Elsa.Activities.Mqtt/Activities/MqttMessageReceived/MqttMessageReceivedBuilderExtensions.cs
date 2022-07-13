using System;
using System.Net.Mqtt;
using System.Runtime.CompilerServices;
using Elsa.Builders;

namespace Elsa.Activities.Mqtt.Activities.MqttMessageReceived
{
    public static class MqttMessageReceivedBuilderExtensions
    {
        public static IActivityBuilder MessageReceived(this IBuilder builder, Action<ISetupActivity<MqttMessageReceived>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder MessageReceived(this IBuilder builder, string topic, string host, int port, string username, string password, MqttQualityOfService qos, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.MessageReceived(setup => setup.WithTopic(topic).WithHost(host).WithPort(port).WithUsername(username).WithPassword(password).WithQualityOfService(qos), lineNumber, sourceFile);
    }
}
