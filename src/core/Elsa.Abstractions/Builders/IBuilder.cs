using System;
using System.Runtime.CompilerServices;
using Elsa.Services;

namespace Elsa.Builders
{
    public interface IBuilder
    {
        /// <summary>
        /// Generic method for adding an arbitrary activity to a <see cref="IBuilder"/>.
        /// </summary>
        /// <typeparam name="T">The activity type.</typeparam>
        /// <param name="activityTypeName">The type name of the activity that needs to be added to the builder</param>
        /// <param name="setup">Set the properties of activity <typeparamref name="T"/></param>
        /// <param name="branch">Optional. Activity to execute after the execution of activity <typeparamref name="T"/></param>
        /// <param name="activity">Optional. Activity to execute after the execution of activity <typeparamref name="T"/></param>
        /// <returns>The <see cref="IActivityBuilder"/> with the <typeparamref name="T"/> activity built onto it.</returns>
        IActivityBuilder Then<T>(string activityTypeName, Action<ISetupActivity<T>>? setup, Action<IActivityBuilder>? branch = default, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default)
            where T : class, IActivity;

        IActivityBuilder Then<T>(string activityTypeName, Action<IActivityBuilder>? branch = default, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) where T : class, IActivity;
    }
}