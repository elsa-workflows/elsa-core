using Elsa.Builders;
using Elsa.Services.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Activities.File
{
    public static class OutFileExtensions
    {
        #region Path
        public static ISetupActivity<OutFile> WithPath(this ISetupActivity<OutFile> setup, Func<ActivityExecutionContext, ValueTask<string>> path) => setup.Set(x => x.Path, path);
        public static ISetupActivity<OutFile> WithPath(this ISetupActivity<OutFile> setup, Func<ActivityExecutionContext, string> path) => setup.Set(x => x.Path, path);
        public static ISetupActivity<OutFile> WithPath(this ISetupActivity<OutFile> setup, Func<ValueTask<string>> path) => setup.Set(x => x.Path, path);
        public static ISetupActivity<OutFile> WithPath(this ISetupActivity<OutFile> setup, Func<string> path) => setup.Set(x => x.Path, path);
        public static ISetupActivity<OutFile> WithPath(this ISetupActivity<OutFile> setup, string path) => setup.Set(x => x.Path, path);
        #endregion

        #region Mode
        public static ISetupActivity<OutFile> WithMode(this ISetupActivity<OutFile> setup, Func<ActivityExecutionContext, ValueTask<CopyMode>> mode) => setup.Set(x => x.Mode, mode);
        public static ISetupActivity<OutFile> WithMode(this ISetupActivity<OutFile> setup, Func<ActivityExecutionContext, CopyMode> mode) => setup.Set(x => x.Mode, mode);
        public static ISetupActivity<OutFile> WithMode(this ISetupActivity<OutFile> setup, Func<ValueTask<CopyMode>> mode) => setup.Set(x => x.Mode, mode);
        public static ISetupActivity<OutFile> WithMode(this ISetupActivity<OutFile> setup, Func<CopyMode> mode) => setup.Set(x => x.Mode, mode);
        public static ISetupActivity<OutFile> WithMode(this ISetupActivity<OutFile> setup, CopyMode mode) => setup.Set(x => x.Mode, mode);
        #endregion

    }
}
