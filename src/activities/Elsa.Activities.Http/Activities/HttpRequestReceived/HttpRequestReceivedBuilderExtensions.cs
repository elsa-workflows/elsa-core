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
    public static class HttpRequestReceivedBuilderExtensions
    {
        public static IActivityBuilder HttpRequestReceived(this IBuilder builder, Action<ISetupActivity<HttpRequestReceived>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder HttpRequestReceived(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<PathString>> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.HttpRequestReceived(setup => setup.Set(x => x.Path, path), lineNumber, sourceFile);

        public static IActivityBuilder HttpRequestReceived(this IBuilder builder, Func<ActivityExecutionContext, PathString> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.HttpRequestReceived(setup => setup.Set(x => x.Path, path), lineNumber, sourceFile);

        public static IActivityBuilder HttpRequestReceived(this IBuilder builder, Func<ValueTask<PathString>> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.HttpRequestReceived(setup => setup.Set(x => x.Path, path), lineNumber, sourceFile);

        public static IActivityBuilder HttpRequestReceived(this IBuilder builder, Func<PathString> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.HttpRequestReceived(setup => setup.Set(x => x.Path, path), lineNumber, sourceFile);

        public static IActivityBuilder HttpRequestReceived(this IBuilder builder, PathString path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.HttpRequestReceived(setup => setup.Set(x => x.Path, path), lineNumber, sourceFile);

        public static IActivityBuilder ReceiveHttpPostRequest<T>(this IBuilder builder, Func<ActivityExecutionContext, PathString> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.HttpRequestReceived(activity => activity.WithPath(path).WithMethod(HttpMethods.Post).WithTargetType<T>(), lineNumber, sourceFile);

        public static IActivityBuilder ReceiveHttpPostRequest<T>(this IBuilder builder, Func<PathString> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.HttpRequestReceived(activity => activity.WithPath(path).WithMethod(HttpMethods.Post).WithTargetType<T>(), lineNumber, sourceFile);

        public static IActivityBuilder ReceiveHttpPostRequest<T>(this IBuilder builder, PathString path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.HttpRequestReceived(activity => activity.WithPath(path).WithMethod(HttpMethods.Post).WithTargetType<T>(), lineNumber, sourceFile);
    }
}