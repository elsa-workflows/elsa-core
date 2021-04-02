using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Primitives
{
    public static class SetVariableExtensions
    {
        public static ISetupActivity<SetVariable> WithVariableName(this ISetupActivity<SetVariable> activity, Func<ActivityExecutionContext, ValueTask<string?>> value) => activity.Set(x => x.VariableName, value);
        public static ISetupActivity<SetVariable> WithVariableName(this ISetupActivity<SetVariable> activity, Func<ValueTask<string?>> value) => activity.Set(x => x.VariableName, value);
        public static ISetupActivity<SetVariable> WithVariableName(this ISetupActivity<SetVariable> activity, Func<ActivityExecutionContext, string?> value) => activity.Set(x => x.VariableName, value);
        public static ISetupActivity<SetVariable> WithVariableName(this ISetupActivity<SetVariable> activity, Func<string?> value) => activity.Set(x => x.VariableName, value);
        public static ISetupActivity<SetVariable> WithVariableName(this ISetupActivity<SetVariable> activity, string? value) => activity.Set(x => x.VariableName, value);
        
        public static ISetupActivity<SetVariable> WithValue(this ISetupActivity<SetVariable> activity, Func<ActivityExecutionContext, ValueTask<object?>> value) => activity.Set(x => x.Value, value);
        public static ISetupActivity<SetVariable> WithValue(this ISetupActivity<SetVariable> activity, Func<ValueTask<object?>> value) => activity.Set(x => x.Value, value);
        public static ISetupActivity<SetVariable> WithValue(this ISetupActivity<SetVariable> activity, Func<ActivityExecutionContext, object?> value) => activity.Set(x => x.Value, value);
        public static ISetupActivity<SetVariable> WithValue(this ISetupActivity<SetVariable> activity, Func<object?> value) => activity.Set(x => x.Value, value);
        public static ISetupActivity<SetVariable> WithValue(this ISetupActivity<SetVariable> activity, object? value) => activity.Set(x => x.Value, value);
    }
}