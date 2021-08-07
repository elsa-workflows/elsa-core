using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public interface ICompositeActivityBuilder : IActivityBuilder
    {
        IServiceProvider ServiceProvider { get; }
        IReadOnlyCollection<IActivityBuilder> Activities { get; }

        IActivityBuilder New<T>(
            string activityTypeName, 
            IDictionary<string, IActivityPropertyValueProvider>? propertyValueProviders = default,
            IDictionary<string, string>? storageProviders = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default)
            where T : class, IActivity;

        IActivityBuilder New<T>(
            string activityTypeName, 
            Action<ISetupActivity<T>>? setup,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) where T : class, IActivity;

        IActivityBuilder StartWith<T>(
            string activityTypeName, 
            Action<ISetupActivity<T>>? setup,
            Action<IActivityBuilder>? branch = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) where T : class, IActivity;

        IActivityBuilder StartWith<T>(string activityTypeName, Action<IActivityBuilder>? branch = default, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) where T : class, IActivity;

        IActivityBuilder Add<T>(
            string activityTypeName, 
            Action<IActivityBuilder>? branch = default,
            IDictionary<string, IActivityPropertyValueProvider>? propertyValueProviders = default,
            IDictionary<string, string>? storageProviders = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default)
            where T : class, IActivity;

        IConnectionBuilder Connect(
            IActivityBuilder source,
            IActivityBuilder target,
            string outcome = OutcomeNames.Done);

        IConnectionBuilder Connect(
            Func<IActivityBuilder> source,
            Func<IActivityBuilder> target,
            string outcome = OutcomeNames.Done);

        ICompositeActivityBlueprint Build(string activityIdPrefix = "activity");
    }
}