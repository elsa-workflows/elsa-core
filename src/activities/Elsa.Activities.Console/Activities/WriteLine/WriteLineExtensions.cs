using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Console
{
    public static class WriteLineExtensions
    {
        public static ISetupActivity<WriteLine> WithText(this ISetupActivity<WriteLine> writeLine, Func<ActivityExecutionContext, ValueTask<string?>> text) => writeLine.Set(x => x.Text, text);
        public static ISetupActivity<WriteLine> WithText(this ISetupActivity<WriteLine> writeLine, Func<ActivityExecutionContext, string> text) => writeLine.Set(x => x.Text, text);
        public static ISetupActivity<WriteLine> WithText(this ISetupActivity<WriteLine> writeLine, Func<ValueTask<string?>> text) => writeLine.Set(x => x.Text, text);
        public static ISetupActivity<WriteLine> WithText(this ISetupActivity<WriteLine> writeLine, Func<string?> text) => writeLine.Set(x => x.Text, text);
        public static ISetupActivity<WriteLine> WithText(this ISetupActivity<WriteLine> writeLine, string? text) => writeLine.Set(x => x.Text, text);
    }
}