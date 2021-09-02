using Elsa.Builders;
using Elsa.Services.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Activities.File
{
    public static class WatchDirectoryExtensions
    {
        public static ISetupActivity<WatchDirectory> WithPath(this ISetupActivity<WatchDirectory> setup, Func<ActivityExecutionContext, ValueTask<string?>> path) => setup.Set(x => x.Path, path);
        public static ISetupActivity<WatchDirectory> WithPath(this ISetupActivity<WatchDirectory> setup, Func<ActivityExecutionContext, string?> path) => setup.Set(x => x.Path, path);
        public static ISetupActivity<WatchDirectory> WithPath(this ISetupActivity<WatchDirectory> setup, Func<ValueTask<string?>> path) => setup.Set(x => x.Path, path);
        public static ISetupActivity<WatchDirectory> WithPath(this ISetupActivity<WatchDirectory> setup, Func<string?> path) => setup.Set(x => x.Path, path);
        public static ISetupActivity<WatchDirectory> WithPath(this ISetupActivity<WatchDirectory> setup, string? path) => setup.Set(x => x.Path, path);

        public static ISetupActivity<WatchDirectory> WithPattern(this ISetupActivity<WatchDirectory> setup, Func<ActivityExecutionContext, ValueTask<string?>> pattern) => setup.Set(x => x.Pattern, pattern);
        public static ISetupActivity<WatchDirectory> WithPattern(this ISetupActivity<WatchDirectory> setup, Func<ActivityExecutionContext, string?> pattern) => setup.Set(x => x.Pattern, pattern);
        public static ISetupActivity<WatchDirectory> WithPattern(this ISetupActivity<WatchDirectory> setup, Func<ValueTask<string?>> pattern) => setup.Set(x => x.Pattern, pattern);
        public static ISetupActivity<WatchDirectory> WithPattern(this ISetupActivity<WatchDirectory> setup, Func<string?> pattern) => setup.Set(x => x.Pattern, pattern);
        public static ISetupActivity<WatchDirectory> WithPattern(this ISetupActivity<WatchDirectory> setup, string? pattern) => setup.Set(x => x.Pattern, pattern);
    }
}
