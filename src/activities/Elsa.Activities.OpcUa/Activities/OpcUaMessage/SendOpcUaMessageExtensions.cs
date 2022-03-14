using Elsa.Builders;
using Elsa.Services.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elsa.Activities.OpcUa
{
    public static class SendOpcUaMessageExtensions
    {
        public static ISetupActivity<SendOpcUaMessage> WithConnectionString(this ISetupActivity<SendOpcUaMessage> sendMessage, Func<ActivityExecutionContext, ValueTask<string>> value) => sendMessage.Set(x => x.ConnectionString, value!);
        public static ISetupActivity<SendOpcUaMessage> WithConnectionString(this ISetupActivity<SendOpcUaMessage> sendMessage, Func<ValueTask<string>> value) => sendMessage.Set(x => x.ConnectionString, value!);
        public static ISetupActivity<SendOpcUaMessage> WithConnectionString(this ISetupActivity<SendOpcUaMessage> sendMessage, Func<string> value) => sendMessage.Set(x => x.ConnectionString, value!);
        public static ISetupActivity<SendOpcUaMessage> WithConnectionString(this ISetupActivity<SendOpcUaMessage> sendMessage, Func<ActivityExecutionContext, string> value) => sendMessage.Set(x => x.ConnectionString, value!);
        public static ISetupActivity<SendOpcUaMessage> WithConnectionString(this ISetupActivity<SendOpcUaMessage> sendMessage, string value) => sendMessage.Set(x => x.ConnectionString, value!);

        public static ISetupActivity<SendOpcUaMessage> WithTags(this ISetupActivity<SendOpcUaMessage> sendMessage, Func<ActivityExecutionContext, ValueTask<Dictionary<string, string>>> value) => sendMessage.Set(x => x.Tags, value!);
        public static ISetupActivity<SendOpcUaMessage> WithTags(this ISetupActivity<SendOpcUaMessage> sendMessage, Func<ValueTask<Dictionary<string, string>>> value) => sendMessage.Set(x => x.Tags, value!);
        public static ISetupActivity<SendOpcUaMessage> WithTags(this ISetupActivity<SendOpcUaMessage> sendMessage, Func<Dictionary<string, string>> value) => sendMessage.Set(x => x.Tags, value!);
        public static ISetupActivity<SendOpcUaMessage> WithTags(this ISetupActivity<SendOpcUaMessage> sendMessage, Func<ActivityExecutionContext, Dictionary<string, string>> value) => sendMessage.Set(x => x.Tags, value!);
        public static ISetupActivity<SendOpcUaMessage> WithTags(this ISetupActivity<SendOpcUaMessage> sendMessage, Dictionary<string, string> value) => sendMessage.Set(x => x.Tags, value!);

        public static ISetupActivity<SendOpcUaMessage> WithMessage(this ISetupActivity<SendOpcUaMessage> sendMessage, Func<ActivityExecutionContext, ValueTask<string>> value) => sendMessage.Set(x => x.Message, value!);
        public static ISetupActivity<SendOpcUaMessage> WithMessage(this ISetupActivity<SendOpcUaMessage> sendMessage, Func<ValueTask<string>> value) => sendMessage.Set(x => x.Message, value!);
        public static ISetupActivity<SendOpcUaMessage> WithMessage(this ISetupActivity<SendOpcUaMessage> sendMessage, Func<string> value) => sendMessage.Set(x => x.Message, value!);
        public static ISetupActivity<SendOpcUaMessage> WithMessage(this ISetupActivity<SendOpcUaMessage> sendMessage, Func<ActivityExecutionContext, string> value) => sendMessage.Set(x => x.Message, value!);
        public static ISetupActivity<SendOpcUaMessage> WithMessage(this ISetupActivity<SendOpcUaMessage> sendMessage, string value) => sendMessage.Set(x => x.Message, value!);
    }
}
