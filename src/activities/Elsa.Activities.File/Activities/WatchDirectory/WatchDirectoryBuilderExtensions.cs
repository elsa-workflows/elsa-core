using Elsa.Builders;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Elsa.Activities.File
{
    public static class WatchDirectoryBuilderExtensions
    {
        public static IActivityBuilder WatchDirectory(this IActivityBuilder builder, Action<ISetupActivity<WatchDirectory>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) => builder.Then(setup, null, lineNumber, sourceFile);
    }
}
