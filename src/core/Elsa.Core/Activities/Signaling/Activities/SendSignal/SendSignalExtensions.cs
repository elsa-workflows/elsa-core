using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Signaling
{
    public static class SendSignalExtensions
    {
        public static ISetupActivity<SendSignal> WithSignal(this ISetupActivity<SendSignal> activity, Func<ActivityExecutionContext, ValueTask<string?>> value) => activity.Set(x => x.Signal, value);
        public static ISetupActivity<SendSignal> WithSignal(this ISetupActivity<SendSignal> activity, Func<ActivityExecutionContext, string?> value) => activity.Set(x => x.Signal, value);
        public static ISetupActivity<SendSignal> WithSignal(this ISetupActivity<SendSignal> activity, Func<ValueTask<string?>> value) => activity.Set(x => x.Signal, value);
        public static ISetupActivity<SendSignal> WithSignal(this ISetupActivity<SendSignal> activity, Func<string?> value) => activity.Set(x => x.Signal, value);
        public static ISetupActivity<SendSignal> WithSignal(this ISetupActivity<SendSignal> activity, string? value) => activity.Set(x => x.Signal, value);
        
        public static ISetupActivity<SendSignal> WithCorrelationId(this ISetupActivity<SendSignal> activity, Func<ActivityExecutionContext, ValueTask<string?>> value) => activity.Set(x => x.CorrelationId, value);
        public static ISetupActivity<SendSignal> WithCorrelationId(this ISetupActivity<SendSignal> activity, Func<ActivityExecutionContext, string?> value) => activity.Set(x => x.CorrelationId, value);
        public static ISetupActivity<SendSignal> WithCorrelationId(this ISetupActivity<SendSignal> activity, Func<ValueTask<string?>> value) => activity.Set(x => x.CorrelationId, value);
        public static ISetupActivity<SendSignal> WithCorrelationId(this ISetupActivity<SendSignal> activity, Func<string?> value) => activity.Set(x => x.CorrelationId, value);
        public static ISetupActivity<SendSignal> WithCorrelationId(this ISetupActivity<SendSignal> activity, string? value) => activity.Set(x => x.CorrelationId, value);
        
        public static ISetupActivity<SendSignal> WithInput(this ISetupActivity<SendSignal> activity, Func<ActivityExecutionContext, ValueTask<object?>> value) => activity.Set(x => x.Input, value);
        public static ISetupActivity<SendSignal> WithInput(this ISetupActivity<SendSignal> activity, Func<ActivityExecutionContext, object?> value) => activity.Set(x => x.Input, value);
        public static ISetupActivity<SendSignal> WithInput(this ISetupActivity<SendSignal> activity, Func<ValueTask<object?>> value) => activity.Set(x => x.Input, value);
        public static ISetupActivity<SendSignal> WithInput(this ISetupActivity<SendSignal> activity, Func<object?> value) => activity.Set(x => x.Input, value);
        public static ISetupActivity<SendSignal> WithInput(this ISetupActivity<SendSignal> activity, object? value) => activity.Set(x => x.Input, value);
    }
}