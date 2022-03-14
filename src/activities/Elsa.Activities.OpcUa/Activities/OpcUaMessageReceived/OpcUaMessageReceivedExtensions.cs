using Elsa.Builders;
using Elsa.Services.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elsa.Activities.OpcUa
{
    public static class OpcUaMessageReceivedExtensions
    {
        public static ISetupActivity<OpcUaMessageReceived> WithConnectionString(this ISetupActivity<OpcUaMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<string>> value) => messageReceived.Set(x => x.ConnectionString, value!);
        public static ISetupActivity<OpcUaMessageReceived> WithConnectionString(this ISetupActivity<OpcUaMessageReceived> messageReceived, Func<ValueTask<string>> value) => messageReceived.Set(x => x.ConnectionString, value!);
        public static ISetupActivity<OpcUaMessageReceived> WithConnectionString(this ISetupActivity<OpcUaMessageReceived> messageReceived, Func<string> value) => messageReceived.Set(x => x.ConnectionString, value!);
        public static ISetupActivity<OpcUaMessageReceived> WithConnectionString(this ISetupActivity<OpcUaMessageReceived> messageReceived, Func<ActivityExecutionContext, string> value) => messageReceived.Set(x => x.ConnectionString, value!);
        public static ISetupActivity<OpcUaMessageReceived> WithConnectionString(this ISetupActivity<OpcUaMessageReceived> messageReceived, string value) => messageReceived.Set(x => x.ConnectionString, value!);

        public static ISetupActivity<OpcUaMessageReceived> WithTags(this ISetupActivity<OpcUaMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<Dictionary<string, string>>> value) => messageReceived.Set(x => x.Tags, value!);
        public static ISetupActivity<OpcUaMessageReceived> WithTags(this ISetupActivity<OpcUaMessageReceived> messageReceived, Func<ValueTask<Dictionary<string, string>>> value) => messageReceived.Set(x => x.Tags, value!);
        public static ISetupActivity<OpcUaMessageReceived> WithTags(this ISetupActivity<OpcUaMessageReceived> messageReceived, Func<Dictionary<string, string>> value) => messageReceived.Set(x => x.Tags, value!);
        public static ISetupActivity<OpcUaMessageReceived> WithTags(this ISetupActivity<OpcUaMessageReceived> messageReceived, Func<ActivityExecutionContext, Dictionary<string, string>> value) => messageReceived.Set(x => x.Tags, value!);
        public static ISetupActivity<OpcUaMessageReceived> WithTags(this ISetupActivity<OpcUaMessageReceived> messageReceived, Dictionary<string, string> value) => messageReceived.Set(x => x.Tags, value!);

        public static ISetupActivity<OpcUaMessageReceived> WithPublishingInterval(this ISetupActivity<OpcUaMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<int>> value) => messageReceived.Set(x => x.PublishingInterval, value!);
        public static ISetupActivity<OpcUaMessageReceived> WithPublishingInterval(this ISetupActivity<OpcUaMessageReceived> messageReceived, Func<ValueTask<int>> value) => messageReceived.Set(x => x.PublishingInterval, value!);
        public static ISetupActivity<OpcUaMessageReceived> WithPublishingInterval(this ISetupActivity<OpcUaMessageReceived> messageReceived, Func<int> value) => messageReceived.Set(x => x.PublishingInterval, value!);
        public static ISetupActivity<OpcUaMessageReceived> WithPublishingInterval(this ISetupActivity<OpcUaMessageReceived> messageReceived, Func<ActivityExecutionContext, int> value) => messageReceived.Set(x => x.PublishingInterval, value!);
        public static ISetupActivity<OpcUaMessageReceived> WithPublishingInterval(this ISetupActivity<OpcUaMessageReceived> messageReceived, int value) => messageReceived.Set(x => x.PublishingInterval, value!);

        public static ISetupActivity<OpcUaMessageReceived> WithSessionTimeout(this ISetupActivity<OpcUaMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<int>> value) => messageReceived.Set(x => x.SessionTimeout, value!);
        public static ISetupActivity<OpcUaMessageReceived> WithSessionTimeout(this ISetupActivity<OpcUaMessageReceived> messageReceived, Func<ValueTask<int>> value) => messageReceived.Set(x => x.SessionTimeout, value!);
        public static ISetupActivity<OpcUaMessageReceived> WithSessionTimeout(this ISetupActivity<OpcUaMessageReceived> messageReceived, Func<int> value) => messageReceived.Set(x => x.SessionTimeout, value!);
        public static ISetupActivity<OpcUaMessageReceived> WithSessionTimeout(this ISetupActivity<OpcUaMessageReceived> messageReceived, Func<ActivityExecutionContext, int> value) => messageReceived.Set(x => x.SessionTimeout, value!);
        public static ISetupActivity<OpcUaMessageReceived> WithSessionTimeout(this ISetupActivity<OpcUaMessageReceived> messageReceived, int value) => messageReceived.Set(x => x.SessionTimeout, value!);


        public static ISetupActivity<OpcUaMessageReceived> WithOperationTimeout(this ISetupActivity<OpcUaMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<int>> value) => messageReceived.Set(x => x.OperationTimeout, value!);
        public static ISetupActivity<OpcUaMessageReceived> WithOperationTimeout(this ISetupActivity<OpcUaMessageReceived> messageReceived, Func<ValueTask<int>> value) => messageReceived.Set(x => x.OperationTimeout, value!);
        public static ISetupActivity<OpcUaMessageReceived> WithOperationTimeout(this ISetupActivity<OpcUaMessageReceived> messageReceived, Func<int> value) => messageReceived.Set(x => x.OperationTimeout, value!);
        public static ISetupActivity<OpcUaMessageReceived> WithOperationTimeout(this ISetupActivity<OpcUaMessageReceived> messageReceived, Func<ActivityExecutionContext, int> value) => messageReceived.Set(x => x.OperationTimeout, value!);
        public static ISetupActivity<OpcUaMessageReceived> WithOperationTimeout(this ISetupActivity<OpcUaMessageReceived> messageReceived, int value) => messageReceived.Set(x => x.OperationTimeout, value!);


    }
}
