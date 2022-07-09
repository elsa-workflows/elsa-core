using Elsa.Builders;
using Elsa.Services.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Elsa.Activities.File
{
    public static class WatchDirectoryExtensions
    {
        #region ChangeTypes
        public static ISetupActivity<WatchDirectory> WithChangeTypes(this ISetupActivity<WatchDirectory> setup, Func<ActivityExecutionContext, ValueTask<WatcherChangeTypes>> changeTypes) => setup.Set(x => x.ChangeTypes, changeTypes);
        public static ISetupActivity<WatchDirectory> WithChangeTypes(this ISetupActivity<WatchDirectory> setup, Func<ActivityExecutionContext, WatcherChangeTypes> changeTypes) => setup.Set(x => x.ChangeTypes, changeTypes);
        public static ISetupActivity<WatchDirectory> WithChangeTypes(this ISetupActivity<WatchDirectory> setup, Func<ValueTask<WatcherChangeTypes>> changeTypes) => setup.Set(x => x.ChangeTypes, changeTypes);
        public static ISetupActivity<WatchDirectory> WithChangeTypes(this ISetupActivity<WatchDirectory> setup, Func<WatcherChangeTypes> changeTypes) => setup.Set(x => x.ChangeTypes, changeTypes);
        public static ISetupActivity<WatchDirectory> WithChangeTypes(this ISetupActivity<WatchDirectory> setup, WatcherChangeTypes changeTypes) => setup.Set(x => x.ChangeTypes, changeTypes);
        #endregion

        #region NotifyFilters
        public static ISetupActivity<WatchDirectory> WithNotifyFilters(this ISetupActivity<WatchDirectory> setup, Func<ActivityExecutionContext, ValueTask<NotifyFilters>> notifyFilters) => setup.Set(x => x.NotifyFilters, notifyFilters);
        public static ISetupActivity<WatchDirectory> WithNotifyFilters(this ISetupActivity<WatchDirectory> setup, Func<ActivityExecutionContext, NotifyFilters> notifyFilters) => setup.Set(x => x.NotifyFilters, notifyFilters);
        public static ISetupActivity<WatchDirectory> WithNotifyFilters(this ISetupActivity<WatchDirectory> setup, Func<ValueTask<NotifyFilters>> notifyFilters) => setup.Set(x => x.NotifyFilters, notifyFilters);
        public static ISetupActivity<WatchDirectory> WithNotifyFilters(this ISetupActivity<WatchDirectory> setup, Func<NotifyFilters> notifyFilters) => setup.Set(x => x.NotifyFilters, notifyFilters);
        public static ISetupActivity<WatchDirectory> WithNotifyFilters(this ISetupActivity<WatchDirectory> setup, NotifyFilters notifyFilters) => setup.Set(x => x.NotifyFilters, notifyFilters);
        #endregion

        #region Path
        public static ISetupActivity<WatchDirectory> WithPath(this ISetupActivity<WatchDirectory> setup, Func<ActivityExecutionContext, ValueTask<string?>> path) => setup.Set(x => x.Path, path);
        public static ISetupActivity<WatchDirectory> WithPath(this ISetupActivity<WatchDirectory> setup, Func<ActivityExecutionContext, string?> path) => setup.Set(x => x.Path, path);
        public static ISetupActivity<WatchDirectory> WithPath(this ISetupActivity<WatchDirectory> setup, Func<ValueTask<string?>> path) => setup.Set(x => x.Path, path);
        public static ISetupActivity<WatchDirectory> WithPath(this ISetupActivity<WatchDirectory> setup, Func<string?> path) => setup.Set(x => x.Path, path);
        public static ISetupActivity<WatchDirectory> WithPath(this ISetupActivity<WatchDirectory> setup, string? path) => setup.Set(x => x.Path, path);
        #endregion

        #region Pattern
        public static ISetupActivity<WatchDirectory> WithPattern(this ISetupActivity<WatchDirectory> setup, Func<ActivityExecutionContext, ValueTask<string?>> pattern) => setup.Set(x => x.Pattern, pattern);
        public static ISetupActivity<WatchDirectory> WithPattern(this ISetupActivity<WatchDirectory> setup, Func<ActivityExecutionContext, string?> pattern) => setup.Set(x => x.Pattern, pattern);
        public static ISetupActivity<WatchDirectory> WithPattern(this ISetupActivity<WatchDirectory> setup, Func<ValueTask<string?>> pattern) => setup.Set(x => x.Pattern, pattern);
        public static ISetupActivity<WatchDirectory> WithPattern(this ISetupActivity<WatchDirectory> setup, Func<string?> pattern) => setup.Set(x => x.Pattern, pattern);
        public static ISetupActivity<WatchDirectory> WithPattern(this ISetupActivity<WatchDirectory> setup, string? pattern) => setup.Set(x => x.Pattern, pattern);
        #endregion
    }
}
