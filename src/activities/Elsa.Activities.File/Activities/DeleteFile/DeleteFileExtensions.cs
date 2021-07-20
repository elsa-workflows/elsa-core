using Elsa.Builders;
using Elsa.Services.Models;
using System;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.File
{
    public static class DeleteFileExtensions
    {
        public static ISetupActivity<DeleteFile> WithPath(this ISetupActivity<DeleteFile> setup, Func<ActivityExecutionContext, ValueTask<string?>> path) => setup.Set(x => x.Path, path);
        public static ISetupActivity<DeleteFile> WithPath(this ISetupActivity<DeleteFile> setup, Func<ActivityExecutionContext, string?> path) => setup.Set(x => x.Path, path);
        public static ISetupActivity<DeleteFile> WithPath(this ISetupActivity<DeleteFile> setup, Func<ValueTask<string?>> path) => setup.Set(x => x.Path, path);
        public static ISetupActivity<DeleteFile> WithPath(this ISetupActivity<DeleteFile> setup, Func<string?> path) => setup.Set(x => x.Path, path);
        public static ISetupActivity<DeleteFile> WithPath(this ISetupActivity<DeleteFile> setup, string? path) => setup.Set(x => x.Path, path);
    }
}
