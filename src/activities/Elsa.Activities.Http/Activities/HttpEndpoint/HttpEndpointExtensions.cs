using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class HttpEndpointExtensions
    {
        public static ISetupActivity<HttpEndpoint> WithPath(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, ValueTask<PathString>> path) => activity.Set(x => x.Path, path);
        public static ISetupActivity<HttpEndpoint> WithPath(this ISetupActivity<HttpEndpoint> activity, Func<ValueTask<PathString>> path) => activity.Set(x => x.Path, path);
        public static ISetupActivity<HttpEndpoint> WithPath(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, PathString> path) => activity.Set(x => x.Path, path);
        public static ISetupActivity<HttpEndpoint> WithPath(this ISetupActivity<HttpEndpoint> activity, Func<PathString> path) => activity.Set(x => x.Path, path);
        public static ISetupActivity<HttpEndpoint> WithPath(this ISetupActivity<HttpEndpoint> activity, PathString path) => activity.Set(x => x.Path, path);

        public static ISetupActivity<HttpEndpoint> WithMethod(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, ValueTask<string?>> method) => activity.Set(x => x.Method, method);
        public static ISetupActivity<HttpEndpoint> WithMethod(this ISetupActivity<HttpEndpoint> activity, Func<ValueTask<string?>> method) => activity.Set(x => x.Method, method);
        public static ISetupActivity<HttpEndpoint> WithMethod(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, string?> method) => activity.Set(x => x.Method, method);
        public static ISetupActivity<HttpEndpoint> WithMethod(this ISetupActivity<HttpEndpoint> activity, Func<string?> method) => activity.Set(x => x.Method, method);
        public static ISetupActivity<HttpEndpoint> WithMethod(this ISetupActivity<HttpEndpoint> activity, string? method) => activity.Set(x => x.Method, method);

        public static ISetupActivity<HttpEndpoint> WithReadContent(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, ValueTask<bool>> value) => activity.Set(x => x.ReadContent, value);
        public static ISetupActivity<HttpEndpoint> WithReadContent(this ISetupActivity<HttpEndpoint> activity, Func<ValueTask<bool>> value) => activity.Set(x => x.ReadContent, value);
        public static ISetupActivity<HttpEndpoint> WithReadContent(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, bool> value) => activity.Set(x => x.ReadContent, value);
        public static ISetupActivity<HttpEndpoint> WithReadContent(this ISetupActivity<HttpEndpoint> activity, Func<bool> value) => activity.Set(x => x.ReadContent, value);
        public static ISetupActivity<HttpEndpoint> WithReadContent(this ISetupActivity<HttpEndpoint> activity, bool value = true) => activity.Set(x => x.ReadContent, value);

        public static ISetupActivity<HttpEndpoint> WithTargetType(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, ValueTask<Type?>> value) => activity.Set(x => x.TargetType, value).WithReadContent();
        public static ISetupActivity<HttpEndpoint> WithTargetType(this ISetupActivity<HttpEndpoint> activity, Func<ValueTask<Type?>> value) => activity.Set(x => x.TargetType, value).WithReadContent();
        public static ISetupActivity<HttpEndpoint> WithTargetType(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, Type?> value) => activity.Set(x => x.TargetType, value).WithReadContent();
        public static ISetupActivity<HttpEndpoint> WithTargetType(this ISetupActivity<HttpEndpoint> activity, Func<Type?> value) => activity.Set(x => x.TargetType, value).WithReadContent();
        public static ISetupActivity<HttpEndpoint> WithTargetType(this ISetupActivity<HttpEndpoint> activity, Type? value) => activity.Set(x => x.TargetType, value).WithReadContent();
        public static ISetupActivity<HttpEndpoint> WithTargetType<T>(this ISetupActivity<HttpEndpoint> activity) => activity.Set(x => x.TargetType, typeof(T)).WithReadContent();
    }
}