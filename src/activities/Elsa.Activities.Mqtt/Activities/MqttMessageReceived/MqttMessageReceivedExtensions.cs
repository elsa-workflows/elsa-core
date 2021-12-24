using Elsa.Builders;
using Elsa.Services.Models;
using System;
using System.Net.Mqtt;
using System.Threading.Tasks;

namespace Elsa.Activities.Mqtt
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

        public static ISetupActivity<MqttMessageReceived> WithQualityOfService(this ISetupActivity<MqttMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<MqttQualityOfService>> value) => messageReceived.Set(x => x.QualityOfService, value!);
        public static ISetupActivity<MqttMessageReceived> WithQualityOfService(this ISetupActivity<MqttMessageReceived> messageReceived, Func<ValueTask<MqttQualityOfService>> value) => messageReceived.Set(x => x.QualityOfService, value!);
        public static ISetupActivity<MqttMessageReceived> WithQualityOfService(this ISetupActivity<MqttMessageReceived> messageReceived, Func<MqttQualityOfService> value) => messageReceived.Set(x => x.QualityOfService, value!);
        public static ISetupActivity<MqttMessageReceived> WithQualityOfService(this ISetupActivity<MqttMessageReceived> messageReceived, Func<ActivityExecutionContext, MqttQualityOfService> value) => messageReceived.Set(x => x.QualityOfService, value!);
        public static ISetupActivity<MqttMessageReceived> WithQualityOfService(this ISetupActivity<MqttMessageReceived> messageReceived, MqttQualityOfService value) => messageReceived.Set(x => x.QualityOfService, value!);
    }
}
