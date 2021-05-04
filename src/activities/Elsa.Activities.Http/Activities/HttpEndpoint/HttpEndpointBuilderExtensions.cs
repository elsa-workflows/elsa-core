using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class HttpEndpointBuilderExtensions
    {
        public static IActivityBuilder HttpEndpoint(this IBuilder builder, Action<ISetupActivity<HttpEndpoint>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder HttpEndpoint(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<PathString>> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.HttpEndpoint(setup => setup.WithPath(path).WithMethod(HttpMethods.Get), lineNumber, sourceFile);

        public static IActivityBuilder HttpEndpoint(this IBuilder builder, Func<ActivityExecutionContext, PathString> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.HttpEndpoint(setup => setup.WithPath(path).WithMethod(HttpMethods.Get), lineNumber, sourceFile);

        public static IActivityBuilder HttpEndpoint(this IBuilder builder, Func<ValueTask<PathString>> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.HttpEndpoint(setup => setup.WithPath(path).WithMethod(HttpMethods.Get), lineNumber, sourceFile);

        public static IActivityBuilder HttpEndpoint(this IBuilder builder, Func<PathString> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.HttpEndpoint(setup => setup.WithPath(path).WithMethod(HttpMethods.Get), lineNumber, sourceFile);

        public static IActivityBuilder HttpEndpoint(this IBuilder builder, PathString path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.HttpEndpoint(setup => setup.WithPath(path).WithMethod(HttpMethods.Get), lineNumber, sourceFile);

        public static IActivityBuilder HttpEndpoint<T>(this IBuilder builder, Func<ActivityExecutionContext, PathString> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.HttpEndpoint(activity => activity.WithPath(path).WithMethod(HttpMethods.Post).WithTargetType<T>(), lineNumber, sourceFile);

        public static IActivityBuilder HttpEndpoint<T>(this IBuilder builder, Func<PathString> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.HttpEndpoint(activity => activity.WithPath(path).WithMethod(HttpMethods.Post).WithTargetType<T>(), lineNumber, sourceFile);

        public static IActivityBuilder HttpEndpoint<T>(this IBuilder builder, PathString path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.HttpEndpoint(activity => activity.WithPath(path).WithMethod(HttpMethods.Post).WithTargetType<T>(), lineNumber, sourceFile);
    }
}