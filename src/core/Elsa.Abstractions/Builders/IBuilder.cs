using System;
using System.Runtime.CompilerServices;
using Elsa.Services;

namespace Elsa.Builders
{
    public interface IBuilder
    {
        IActivityBuilder Then<T>(string activityTypeName, Action<ISetupActivity<T>>? setup, Action<IActivityBuilder>? branch = default, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default)
            where T : class, IActivity;

        IActivityBuilder Then<T>(string activityTypeName, Action<IActivityBuilder>? branch = default, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) where T : class, IActivity;
    }
}