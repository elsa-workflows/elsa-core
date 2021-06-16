using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Elsa.Services;

// ReSharper disable ExplicitCallerInfoArgument
namespace Elsa.Builders
{
    public static class CompositeActivityBuilderExtensions
    {
        public static IActivityBuilder New<T>(
            this ICompositeActivityBuilder compositeActivityBuilder,
            IDictionary<string, IActivityPropertyValueProvider>? propertyValueProviders = default,
            IDictionary<string, string>? storageProviders = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default)
            where T : class, IActivity => compositeActivityBuilder.New<T>(typeof(T).Name, propertyValueProviders, storageProviders, lineNumber, sourceFile);

        public static IActivityBuilder New<T>(
            this ICompositeActivityBuilder compositeActivityBuilder,
            Action<ISetupActivity<T>>? setup,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) where T : class, IActivity => compositeActivityBuilder.New<T>(typeof(T).Name, setup, lineNumber, sourceFile);

        public static IActivityBuilder StartWith<T>(
            this ICompositeActivityBuilder compositeActivityBuilder,
            Action<ISetupActivity<T>>? setup,
            Action<IActivityBuilder>? branch = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) where T : class, IActivity => compositeActivityBuilder.StartWith(typeof(T).Name, setup, branch, lineNumber, sourceFile);

        public static IActivityBuilder StartWith<T>(
            this ICompositeActivityBuilder compositeActivityBuilder,
            Action<IActivityBuilder>? branch = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) where T : class, IActivity => compositeActivityBuilder.StartWith<T>(typeof(T).Name, branch, lineNumber, sourceFile);

        public static IActivityBuilder Add<T>(
            this ICompositeActivityBuilder compositeActivityBuilder,
            Action<ISetupActivity<T>>? setup,
            Action<IActivityBuilder>? branch = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) where T : class, IActivity => compositeActivityBuilder.Add(typeof(T).Name, setup, branch, lineNumber, sourceFile);

        public static IActivityBuilder Add<T>(
            this ICompositeActivityBuilder compositeActivityBuilder,
            Action<IActivityBuilder>? branch = default,
            IDictionary<string, IActivityPropertyValueProvider>? propertyValueProviders = default,
            IDictionary<string, string>? storageProviders = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default)
            where T : class, IActivity => compositeActivityBuilder.Add<T>(typeof(T).Name, branch, propertyValueProviders, storageProviders, lineNumber, sourceFile);
    }
}