using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Rebus
{
    public static class RebusMessageReceivedExtensions
    {
        public static ISetupActivity<RebusMessageReceived> WithMessageType(this ISetupActivity<RebusMessageReceived> activity, Func<ActivityExecutionContext, ValueTask<Type>> value) => activity.Set(x => x.MessageType, value!);

        public static ISetupActivity<RebusMessageReceived> WithMessageType(this ISetupActivity<RebusMessageReceived> activity, Func<ValueTask<Type>> value) => activity.Set(x => x.MessageType, value!);

        public static ISetupActivity<RebusMessageReceived> WithMessageType(this ISetupActivity<RebusMessageReceived> activity, Func<Type> value) => activity.Set(x => x.MessageType, value!);

        public static ISetupActivity<RebusMessageReceived> WithMessageType(this ISetupActivity<RebusMessageReceived> activity, Func<ActivityExecutionContext, Type> value) => activity.Set(x => x.MessageType, value!);

        public static ISetupActivity<RebusMessageReceived> WithMessageType(this ISetupActivity<RebusMessageReceived> activity, Type value) => activity.Set(x => x.MessageType, value!);
    }
}