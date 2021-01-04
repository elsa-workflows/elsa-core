using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class FinishExtensions
    {
        public static ISetupActivity<Finish> WithOutput(this ISetupActivity<Finish> activity, Func<ActivityExecutionContext, ValueTask<object?>> value) => activity.Set(x => x.OutputValue, value);
        public static ISetupActivity<Finish> WithOutput(this ISetupActivity<Finish> activity, Func<ActivityExecutionContext, object?> value) => activity.Set(x => x.OutputValue, value);
        public static ISetupActivity<Finish> WithOutput(this ISetupActivity<Finish> activity, Func<object?> value) => activity.Set(x => x.OutputValue, value);
        public static ISetupActivity<Finish> WithOutput(this ISetupActivity<Finish> activity, object? value) => activity.Set(x => x.OutputValue, value);
        
        public static ISetupActivity<Finish> WithOutcome(this ISetupActivity<Finish> activity, Func<ActivityExecutionContext, ValueTask<string?>> value) => activity.Set(x => x.OutcomeName, value);
        public static ISetupActivity<Finish> WithOutcome(this ISetupActivity<Finish> activity, Func<ActivityExecutionContext, string?> value) => activity.Set(x => x.OutcomeName, value);
        public static ISetupActivity<Finish> WithOutcome(this ISetupActivity<Finish> activity, Func<string?> value) => activity.Set(x => x.OutcomeName, value);
        public static ISetupActivity<Finish> WithOutcome(this ISetupActivity<Finish> activity, string? value) => activity.Set(x => x.OutcomeName, value);
    }
}