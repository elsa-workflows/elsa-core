using Elsa.Builders;
using Elsa.Services.Models;
using System;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.File
{
    public static class ReadFileExtensions
    {
        public static ISetupActivity<ReadFile> WithPath(this ISetupActivity<ReadFile> setup, Func<ActivityExecutionContext, ValueTask<string?>> path) => setup.Set(x => x.Path, path);
        public static ISetupActivity<ReadFile> WithPath(this ISetupActivity<ReadFile> setup, Func<ActivityExecutionContext, string?> path) => setup.Set(x => x.Path, path);
        public static ISetupActivity<ReadFile> WithPath(this ISetupActivity<ReadFile> setup, Func<ValueTask<string?>> path) => setup.Set(x => x.Path, path);
        public static ISetupActivity<ReadFile> WithPath(this ISetupActivity<ReadFile> setup, Func<string?> path) => setup.Set(x => x.Path, path);
        public static ISetupActivity<ReadFile> WithPath(this ISetupActivity<ReadFile> setup, string? path) => setup.Set(x => x.Path, path);
    }
}