using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

namespace Elsa.Activities.Primitives
{
    public static class SetNameExtensions
    {
        public static ISetupActivity<SetName> WithValue(this ISetupActivity<SetName> activity, Func<ActivityExecutionContext, ValueTask<string?>> value) => activity.Set(x => x.Value, value);
        public static ISetupActivity<SetName> WithValue(this ISetupActivity<SetName> activity, Func<ValueTask<string?>> value) => activity.Set(x => x.Value, value);
        public static ISetupActivity<SetName> WithValue(this ISetupActivity<SetName> activity, Func<ActivityExecutionContext, string?> value) => activity.Set(x => x.Value, value);
        public static ISetupActivity<SetName> WithValue(this ISetupActivity<SetName> activity, Func<string?> value) => activity.Set(x => x.Value, value);
        public static ISetupActivity<SetName> WithValue(this ISetupActivity<SetName> activity, string? value) => activity.Set(x => x.Value, value);
    }
}