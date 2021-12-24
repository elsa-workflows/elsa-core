using Elsa.Builders;
using Elsa.Services.Models;
using System;
using System.Net.Mqtt;
using System.Threading.Tasks;

namespace Elsa.Activities.Mqtt
{
    public static class SendMqttMessageExtensions
    {
        public static ISetupActivity<SendMqttMessage> WithTopic(this ISetupActivity<SendMqttMessage> messageReceived, Func<ActivityExecutionContext, ValueTask<string>> value) => messageReceived.Set(x => x.Topic, value!);
        public static ISetupActivity<SendMqttMessage> WithTopic(this ISetupActivity<SendMqttMessage> messageReceived, Func<ValueTask<string>> value) => messageReceived.Set(x => x.Topic, value!);
        public static ISetupActivity<SendMqttMessage> WithTopic(this ISetupActivity<SendMqttMessage> messageReceived, Func<string> value) => messageReceived.Set(x => x.Topic, value!);
        public static ISetupActivity<SendMqttMessage> WithTopic(this ISetupActivity<SendMqttMessage> messageReceived, Func<ActivityExecutionContext, string> value) => messageReceived.Set(x => x.Topic, value!);
        public static ISetupActivity<SendMqttMessage> WithTopic(this ISetupActivity<SendMqttMessage> messageReceived, string value) => messageReceived.Set(x => x.Topic, value!);

        public static ISetupActivity<SendMqttMessage> WithHost(this ISetupActivity<SendMqttMessage> messageReceived, Func<ActivityExecutionContext, ValueTask<string>> value) => messageReceived.Set(x => x.Host, value!);
        public static ISetupActivity<SendMqttMessage> WithHost(this ISetupActivity<SendMqttMessage> messageReceived, Func<ValueTask<string>> value) => messageReceived.Set(x => x.Host, value!);
        public static ISetupActivity<SendMqttMessage> WithHost(this ISetupActivity<SendMqttMessage> messageReceived, Func<string> value) => messageReceived.Set(x => x.Host, value!);
        public static ISetupActivity<SendMqttMessage> WithHost(this ISetupActivity<SendMqttMessage> messageReceived, Func<ActivityExecutionContext, string> value) => messageReceived.Set(x => x.Host, value!);
        public static ISetupActivity<SendMqttMessage> WithHost(this ISetupActivity<SendMqttMessage> messageReceived, string value) => messageReceived.Set(x => x.Host, value!);

        public static ISetupActivity<SendMqttMessage> WithPort(this ISetupActivity<SendMqttMessage> messageReceived, Func<ActivityExecutionContext, ValueTask<int>> value) => messageReceived.Set(x => x.Port, value!);
        public static ISetupActivity<SendMqttMessage> WithPort(this ISetupActivity<SendMqttMessage> messageReceived, Func<ValueTask<int>> value) => messageReceived.Set(x => x.Port, value!);
        public static ISetupActivity<SendMqttMessage> WithPort(this ISetupActivity<SendMqttMessage> messageReceived, Func<int> value) => messageReceived.Set(x => x.Port, value!);
        public static ISetupActivity<SendMqttMessage> WithPort(this ISetupActivity<SendMqttMessage> messageReceived, Func<ActivityExecutionContext, int> value) => messageReceived.Set(x => x.Port, value!);
        public static ISetupActivity<SendMqttMessage> WithPort(this ISetupActivity<SendMqttMessage> messageReceived, int value) => messageReceived.Set(x => x.Port, value!);

        public static ISetupActivity<SendMqttMessage> WithUsername(this ISetupActivity<SendMqttMessage> messageReceived, Func<ActivityExecutionContext, ValueTask<string>> value) => messageReceived.Set(x => x.Username, value!);
        public static ISetupActivity<SendMqttMessage> WithUsername(this ISetupActivity<SendMqttMessage> messageReceived, Func<ValueTask<string>> value) => messageReceived.Set(x => x.Username, value!);
        public static ISetupActivity<SendMqttMessage> WithUsername(this ISetupActivity<SendMqttMessage> messageReceived, Func<string> value) => messageReceived.Set(x => x.Username, value!);
        public static ISetupActivity<SendMqttMessage> WithUsername(this ISetupActivity<SendMqttMessage> messageReceived, Func<ActivityExecutionContext, string> value) => messageReceived.Set(x => x.Username, value!);
        public static ISetupActivity<SendMqttMessage> WithUsername(this ISetupActivity<SendMqttMessage> messageReceived, string value) => messageReceived.Set(x => x.Username, value!);

        public static ISetupActivity<SendMqttMessage> WithPassword(this ISetupActivity<SendMqttMessage> messageReceived, Func<ActivityExecutionContext, ValueTask<string>> value) => messageReceived.Set(x => x.Password, value!);
        public static ISetupActivity<SendMqttMessage> WithPassword(this ISetupActivity<SendMqttMessage> messageReceived, Func<ValueTask<string>> value) => messageReceived.Set(x => x.Password, value!);
        public static ISetupActivity<SendMqttMessage> WithPassword(this ISetupActivity<SendMqttMessage> messageReceived, Func<string> value) => messageReceived.Set(x => x.Password, value!);
        public static ISetupActivity<SendMqttMessage> WithPassword(this ISetupActivity<SendMqttMessage> messageReceived, Func<ActivityExecutionContext, string> value) => messageReceived.Set(x => x.Password, value!);
        public static ISetupActivity<SendMqttMessage> WithPassword(this ISetupActivity<SendMqttMessage> messageReceived, string value) => messageReceived.Set(x => x.Password, value!);

        public static ISetupActivity<SendMqttMessage> WithMessage(this ISetupActivity<SendMqttMessage> messageReceived, Func<ActivityExecutionContext, ValueTask<string>> value) => messageReceived.Set(x => x.Message, value!);
        public static ISetupActivity<SendMqttMessage> WithMessage(this ISetupActivity<SendMqttMessage> messageReceived, Func<ValueTask<string>> value) => messageReceived.Set(x => x.Message, value!);
        public static ISetupActivity<SendMqttMessage> WithMessage(this ISetupActivity<SendMqttMessage> messageReceived, Func<string> value) => messageReceived.Set(x => x.Message, value!);
        public static ISetupActivity<SendMqttMessage> WithMessage(this ISetupActivity<SendMqttMessage> messageReceived, Func<ActivityExecutionContext, string> value) => messageReceived.Set(x => x.Message, value!);
        public static ISetupActivity<SendMqttMessage> WithMessage(this ISetupActivity<SendMqttMessage> messageReceived, string value) => messageReceived.Set(x => x.Message, value!);

        public static ISetupActivity<SendMqttMessage> WithQualityOfService(this ISetupActivity<SendMqttMessage> messageReceived, Func<ActivityExecutionContext, ValueTask<MqttQualityOfService>> value) => messageReceived.Set(x => x.QualityOfService, value!);
        public static ISetupActivity<SendMqttMessage> WithQualityOfService(this ISetupActivity<SendMqttMessage> messageReceived, Func<ValueTask<MqttQualityOfService>> value) => messageReceived.Set(x => x.QualityOfService, value!);
        public static ISetupActivity<SendMqttMessage> WithQualityOfService(this ISetupActivity<SendMqttMessage> messageReceived, Func<MqttQualityOfService> value) => messageReceived.Set(x => x.QualityOfService, value!);
        public static ISetupActivity<SendMqttMessage> WithQualityOfService(this ISetupActivity<SendMqttMessage> messageReceived, Func<ActivityExecutionContext, MqttQualityOfService> value) => messageReceived.Set(x => x.QualityOfService, value!);
        public static ISetupActivity<SendMqttMessage> WithQualityOfService(this ISetupActivity<SendMqttMessage> messageReceived, MqttQualityOfService value) => messageReceived.Set(x => x.QualityOfService, value!);
    }
}
