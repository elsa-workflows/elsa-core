using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.JavaScript
{
    public static class RunJavaScriptExtensions
    {
        public static ISetupActivity<RunJavaScript> WithScript(this ISetupActivity<RunJavaScript> activity, Func<ActivityExecutionContext, ValueTask<string?>> value) => activity.Set(x => x.Script, value);
        public static ISetupActivity<RunJavaScript> WithScript(this ISetupActivity<RunJavaScript> activity, Func<ActivityExecutionContext, string?> value) => activity.Set(x => x.Script, value);
        public static ISetupActivity<RunJavaScript> WithScript(this ISetupActivity<RunJavaScript> activity, Func<ValueTask<string?>> value) => activity.Set(x => x.Script, value);
        public static ISetupActivity<RunJavaScript> WithScript(this ISetupActivity<RunJavaScript> activity, Func<string?> value) => activity.Set(x => x.Script, value);
        public static ISetupActivity<RunJavaScript> WithScript(this ISetupActivity<RunJavaScript> activity, string? value) => activity.Set(x => x.Script, value);
    }
}