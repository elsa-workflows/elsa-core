using System;
using System.Collections.Generic;
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
        
        public static ISetupActivity<Finish> WithOutcome(this ISetupActivity<Finish> activity, Func<ActivityExecutionContext, ValueTask<IEnumerable<string>?>> value) => activity.Set(x => x.OutcomeNames, value);
        public static ISetupActivity<Finish> WithOutcome(this ISetupActivity<Finish> activity, Func<ActivityExecutionContext, IEnumerable<string>> value) => activity.Set(x => x.OutcomeNames, value);
        public static ISetupActivity<Finish> WithOutcome(this ISetupActivity<Finish> activity, Func<IEnumerable<string>> value) => activity.Set(x => x.OutcomeNames, value);
        public static ISetupActivity<Finish> WithOutcome(this ISetupActivity<Finish> activity, IEnumerable<string> value) => activity.Set(x => x.OutcomeNames, value);
    }
}