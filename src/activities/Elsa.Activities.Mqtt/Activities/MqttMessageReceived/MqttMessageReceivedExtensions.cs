using System;
using MQTTnet.Protocol;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

namespace Elsa.Activities.Mqtt.Activities.MqttMessageReceived
{
    public static class MqttMessageReceivedExtensions
    {
        public static ISetupActivity<MqttMessageReceived> WithTopic(this ISetupActivity<MqttMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<string>> value) => messageReceived.Set(x => x.Topic, value!);
        public static ISetupActivity<MqttMessageReceived> WithTopic(this ISetupActivity<MqttMessageReceived> messageReceived, Func<ValueTask<string>> value) => messageReceived.Set(x => x.Topic, value!);
        public static ISetupActivity<MqttMessageReceived> WithTopic(this ISetupActivity<MqttMessageReceived> messageReceived, Func<string> value) => messageReceived.Set(x => x.Topic, value!);
        public static ISetupActivity<MqttMessageReceived> WithTopic(this ISetupActivity<MqttMessageReceived> messageReceived, Func<ActivityExecutionContext, string> value) => messageReceived.Set(x => x.Topic, value!);
        public static ISetupActivity<MqttMessageReceived> WithTopic(this ISetupActivity<MqttMessageReceived> messageReceived, string value) => messageReceived.Set(x => x.Topic, value!);

        public static ISetupActivity<MqttMessageReceived> WithHost(this ISetupActivity<MqttMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<string>> value) => messageReceived.Set(x => x.Host, value!);
        public static ISetupActivity<MqttMessageReceived> WithHost(this ISetupActivity<MqttMessageReceived> messageReceived, Func<ValueTask<string>> value) => messageReceived.Set(x => x.Host, value!);
        public static ISetupActivity<MqttMessageReceived> WithHost(this ISetupActivity<MqttMessageReceived> messageReceived, Func<string> value) => messageReceived.Set(x => x.Host, value!);
        public static ISetupActivity<MqttMessageReceived> WithHost(this ISetupActivity<MqttMessageReceived> messageReceived, Func<ActivityExecutionContext, string> value) => messageReceived.Set(x => x.Host, value!);
        public static ISetupActivity<MqttMessageReceived> WithHost(this ISetupActivity<MqttMessageReceived> messageReceived, string value) => messageReceived.Set(x => x.Host, value!);

        public static ISetupActivity<MqttMessageReceived> WithPort(this ISetupActivity<MqttMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<int>> value) => messageReceived.Set(x => x.Port, value!);
        public static ISetupActivity<MqttMessageReceived> WithPort(this ISetupActivity<MqttMessageReceived> messageReceived, Func<ValueTask<int>> value) => messageReceived.Set(x => x.Port, value!);
        public static ISetupActivity<MqttMessageReceived> WithPort(this ISetupActivity<MqttMessageReceived> messageReceived, Func<int> value) => messageReceived.Set(x => x.Port, value!);
        public static ISetupActivity<MqttMessageReceived> WithPort(this ISetupActivity<MqttMessageReceived> messageReceived, Func<ActivityExecutionContext, int> value) => messageReceived.Set(x => x.Port, value!);
        public static ISetupActivity<MqttMessageReceived> WithPort(this ISetupActivity<MqttMessageReceived> messageReceived, int value) => messageReceived.Set(x => x.Port, value!);

        public static ISetupActivity<MqttMessageReceived> WithUsername(this ISetupActivity<MqttMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<string>> value) => messageReceived.Set(x => x.Username, value!);
        public static ISetupActivity<MqttMessageReceived> WithUsername(this ISetupActivity<MqttMessageReceived> messageReceived, Func<ValueTask<string>> value) => messageReceived.Set(x => x.Username, value!);
        public static ISetupActivity<MqttMessageReceived> WithUsername(this ISetupActivity<MqttMessageReceived> messageReceived, Func<string> value) => messageReceived.Set(x => x.Username, value!);
        public static ISetupActivity<MqttMessageReceived> WithUsername(this ISetupActivity<MqttMessageReceived> messageReceived, Func<ActivityExecutionContext, string> value) => messageReceived.Set(x => x.Username, value!);
        public static ISetupActivity<MqttMessageReceived> WithUsername(this ISetupActivity<MqttMessageReceived> messageReceived, string value) => messageReceived.Set(x => x.Username, value!);

        public static ISetupActivity<MqttMessageReceived> WithPassword(this ISetupActivity<MqttMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<string>> value) => messageReceived.Set(x => x.Password, value!);
        public static ISetupActivity<MqttMessageReceived> WithPassword(this ISetupActivity<MqttMessageReceived> messageReceived, Func<ValueTask<string>> value) => messageReceived.Set(x => x.Password, value!);
        public static ISetupActivity<MqttMessageReceived> WithPassword(this ISetupActivity<MqttMessageReceived> messageReceived, Func<string> value) => messageReceived.Set(x => x.Password, value!);
        public static ISetupActivity<MqttMessageReceived> WithPassword(this ISetupActivity<MqttMessageReceived> messageReceived, Func<ActivityExecutionContext, string> value) => messageReceived.Set(x => x.Password, value!);
        public static ISetupActivity<MqttMessageReceived> WithPassword(this ISetupActivity<MqttMessageReceived> messageReceived, string value) => messageReceived.Set(x => x.Password, value!);

        public static ISetupActivity<MqttMessageReceived> WithQualityOfService(this ISetupActivity<MqttMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<MqttQualityOfServiceLevel>> value) => messageReceived.Set(x => x.QualityOfService, value!);
        public static ISetupActivity<MqttMessageReceived> WithQualityOfService(this ISetupActivity<MqttMessageReceived> messageReceived, Func<ValueTask<MqttQualityOfServiceLevel>> value) => messageReceived.Set(x => x.QualityOfService, value!);
        public static ISetupActivity<MqttMessageReceived> WithQualityOfService(this ISetupActivity<MqttMessageReceived> messageReceived, Func<MqttQualityOfServiceLevel> value) => messageReceived.Set(x => x.QualityOfService, value!);
        public static ISetupActivity<MqttMessageReceived> WithQualityOfService(this ISetupActivity<MqttMessageReceived> messageReceived, Func<ActivityExecutionContext, MqttQualityOfServiceLevel> value) => messageReceived.Set(x => x.QualityOfService, value!);
        public static ISetupActivity<MqttMessageReceived> WithQualityOfService(this ISetupActivity<MqttMessageReceived> messageReceived, MqttQualityOfServiceLevel value) => messageReceived.Set(x => x.QualityOfService, value!);
    }
}
