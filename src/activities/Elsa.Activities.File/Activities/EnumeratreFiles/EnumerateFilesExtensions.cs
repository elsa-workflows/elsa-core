using Elsa.Builders;
using Elsa.Services.Models;
using System;
using System.IO;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.File
{
    public static class EnumerateFilesExtensions
    {
        #region Path
        public static ISetupActivity<EnumerateFiles> WithPath(this ISetupActivity<EnumerateFiles> setup, Func<ActivityExecutionContext, ValueTask<string?>> path) => setup.Set(x => x.Path, path);
        public static ISetupActivity<EnumerateFiles> WithPath(this ISetupActivity<EnumerateFiles> setup, Func<ActivityExecutionContext, string?> path) => setup.Set(x => x.Path, path);
        public static ISetupActivity<EnumerateFiles> WithPath(this ISetupActivity<EnumerateFiles> setup, Func<ValueTask<string?>> path) => setup.Set(x => x.Path, path);
        public static ISetupActivity<EnumerateFiles> WithPath(this ISetupActivity<EnumerateFiles> setup, Func<string?> path) => setup.Set(x => x.Path, path);
        public static ISetupActivity<EnumerateFiles> WithPath(this ISetupActivity<EnumerateFiles> setup, string? path) => setup.Set(x => x.Path, path);
        #endregion

        #region Pattern
        public static ISetupActivity<EnumerateFiles> WithPattern(this ISetupActivity<EnumerateFiles> setup, Func<ActivityExecutionContext, ValueTask<string?>> pattern) => setup.Set(x => x.Pattern, pattern);
        public static ISetupActivity<EnumerateFiles> WithPattern(this ISetupActivity<EnumerateFiles> setup, Func<ActivityExecutionContext, string?> pattern) => setup.Set(x => x.Pattern, pattern);
        public static ISetupActivity<EnumerateFiles> WithPattern(this ISetupActivity<EnumerateFiles> setup, Func<ValueTask<string?>> pattern) => setup.Set(x => x.Pattern, pattern);
        public static ISetupActivity<EnumerateFiles> WithPattern(this ISetupActivity<EnumerateFiles> setup, Func<string?> pattern) => setup.Set(x => x.Pattern, pattern);
        public static ISetupActivity<EnumerateFiles> WithPattern(this ISetupActivity<EnumerateFiles> setup, string? pattern) => setup.Set(x => x.Pattern, pattern);
        #endregion

        #region IgnoreInaccessible
        public static ISetupActivity<EnumerateFiles> WithIgnoreInaccessible(this ISetupActivity<EnumerateFiles> setup, Func<ActivityExecutionContext, ValueTask<bool>> ignoreInaccessible) => setup.Set(x => x.IgnoreInaccessible, ignoreInaccessible);
        public static ISetupActivity<EnumerateFiles> WithIgnoreInaccessible(this ISetupActivity<EnumerateFiles> setup, Func<ActivityExecutionContext, bool> ignoreInaccessible) => setup.Set(x => x.IgnoreInaccessible, ignoreInaccessible);
        public static ISetupActivity<EnumerateFiles> WithIgnoreInaccessible(this ISetupActivity<EnumerateFiles> setup, Func<ValueTask<bool>> ignoreInaccessible) => setup.Set(x => x.IgnoreInaccessible, ignoreInaccessible);
        public static ISetupActivity<EnumerateFiles> WithIgnoreInaccessible(this ISetupActivity<EnumerateFiles> setup, Func<bool> ignoreInaccessible) => setup.Set(x => x.IgnoreInaccessible, ignoreInaccessible);
        public static ISetupActivity<EnumerateFiles> WithIgnoreInaccessible(this ISetupActivity<EnumerateFiles> setup, bool ignoreInaccessible) => setup.Set(x => x.IgnoreInaccessible, ignoreInaccessible);
        #endregion

        #region MatchCasing
        public static ISetupActivity<EnumerateFiles> WithMatchCasing(this ISetupActivity<EnumerateFiles> setup, Func<ActivityExecutionContext, ValueTask<MatchCasing>> matchCasing) => setup.Set(x => x.MatchCasing, matchCasing);
        public static ISetupActivity<EnumerateFiles> WithMatchCasing(this ISetupActivity<EnumerateFiles> setup, Func<ActivityExecutionContext, MatchCasing> matchCasing) => setup.Set(x => x.MatchCasing, matchCasing);
        public static ISetupActivity<EnumerateFiles> WithMatchCasing(this ISetupActivity<EnumerateFiles> setup, Func<ValueTask<MatchCasing>> matchCasing) => setup.Set(x => x.MatchCasing, matchCasing);
        public static ISetupActivity<EnumerateFiles> WithMatchCasing(this ISetupActivity<EnumerateFiles> setup, Func<MatchCasing> matchCasing) => setup.Set(x => x.MatchCasing, matchCasing);
        public static ISetupActivity<EnumerateFiles> WithMatchCasing(this ISetupActivity<EnumerateFiles> setup, MatchCasing matchCasing) => setup.Set(x => x.MatchCasing, matchCasing);
        #endregion

        #region SubDirectories
        public static ISetupActivity<EnumerateFiles> WithSubDirectories(this ISetupActivity<EnumerateFiles> setup, Func<ActivityExecutionContext, ValueTask<bool>> subDirectories) => setup.Set(x => x.SubDirectories, subDirectories);
        public static ISetupActivity<EnumerateFiles> WithSubDirectories(this ISetupActivity<EnumerateFiles> setup, Func<ActivityExecutionContext, bool> subDirectories) => setup.Set(x => x.SubDirectories, subDirectories);
        public static ISetupActivity<EnumerateFiles> WithSubDirectories(this ISetupActivity<EnumerateFiles> setup, Func<ValueTask<bool>> subDirectories) => setup.Set(x => x.SubDirectories, subDirectories);
        public static ISetupActivity<EnumerateFiles> WithSubDirectories(this ISetupActivity<EnumerateFiles> setup, Func<bool> subDirectories) => setup.Set(x => x.SubDirectories, subDirectories);
        public static ISetupActivity<EnumerateFiles> WithSubDirectories(this ISetupActivity<EnumerateFiles> setup, bool subDirectories) => setup.Set(x => x.SubDirectories, subDirectories);
        #endregion
    }
}
