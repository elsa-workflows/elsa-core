using System;
using System.Runtime.CompilerServices;
using Elsa.Services;

namespace Elsa.Builders
{
    public interface IBuilder
    {
        IActivityBuilder Then<T>(Action<ISetupActivity<T>>? setup, Action<IActivityBuilder>? branch = default, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) where T : class, IActivity;
        IActivityBuilder Then<T>(Action<IActivityBuilder>? branch = default, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) where T : class, IActivity;
    }
}