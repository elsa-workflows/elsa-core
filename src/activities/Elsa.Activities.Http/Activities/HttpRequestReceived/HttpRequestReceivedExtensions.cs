using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class HttpRequestReceivedExtensions
    {
        public static ISetupActivity<HttpRequestReceived> WithPath(this ISetupActivity<HttpRequestReceived> activity, Func<ActivityExecutionContext, ValueTask<PathString>> path) => activity.Set(x => x.Path, path);
        public static ISetupActivity<HttpRequestReceived> WithPath(this ISetupActivity<HttpRequestReceived> activity, Func<ValueTask<PathString>> path) => activity.Set(x => x.Path, path);
        public static ISetupActivity<HttpRequestReceived> WithPath(this ISetupActivity<HttpRequestReceived> activity, Func<ActivityExecutionContext, PathString> path) => activity.Set(x => x.Path, path);
        public static ISetupActivity<HttpRequestReceived> WithPath(this ISetupActivity<HttpRequestReceived> activity, Func<PathString> path) => activity.Set(x => x.Path, path);
        public static ISetupActivity<HttpRequestReceived> WithPath(this ISetupActivity<HttpRequestReceived> activity, PathString path) => activity.Set(x => x.Path, path);

        public static ISetupActivity<HttpRequestReceived> WithMethod(this ISetupActivity<HttpRequestReceived> activity, Func<ActivityExecutionContext, ValueTask<string?>> method) => activity.Set(x => x.Method, method);
        public static ISetupActivity<HttpRequestReceived> WithMethod(this ISetupActivity<HttpRequestReceived> activity, Func<ValueTask<string?>> method) => activity.Set(x => x.Method, method);
        public static ISetupActivity<HttpRequestReceived> WithMethod(this ISetupActivity<HttpRequestReceived> activity, Func<ActivityExecutionContext, string?> method) => activity.Set(x => x.Method, method);
        public static ISetupActivity<HttpRequestReceived> WithMethod(this ISetupActivity<HttpRequestReceived> activity, Func<string?> method) => activity.Set(x => x.Method, method);
        public static ISetupActivity<HttpRequestReceived> WithMethod(this ISetupActivity<HttpRequestReceived> activity, string? method) => activity.Set(x => x.Method, method);

        public static ISetupActivity<HttpRequestReceived> WithReadContent(this ISetupActivity<HttpRequestReceived> activity, Func<ActivityExecutionContext, ValueTask<bool>> value) => activity.Set(x => x.ReadContent, value);
        public static ISetupActivity<HttpRequestReceived> WithReadContent(this ISetupActivity<HttpRequestReceived> activity, Func<ValueTask<bool>> value) => activity.Set(x => x.ReadContent, value);
        public static ISetupActivity<HttpRequestReceived> WithReadContent(this ISetupActivity<HttpRequestReceived> activity, Func<ActivityExecutionContext, bool> value) => activity.Set(x => x.ReadContent, value);
        public static ISetupActivity<HttpRequestReceived> WithReadContent(this ISetupActivity<HttpRequestReceived> activity, Func<bool> value) => activity.Set(x => x.ReadContent, value);
        public static ISetupActivity<HttpRequestReceived> WithReadContent(this ISetupActivity<HttpRequestReceived> activity, bool value = true) => activity.Set(x => x.ReadContent, value);

        public static ISetupActivity<HttpRequestReceived> WithTargetType(this ISetupActivity<HttpRequestReceived> activity, Func<ActivityExecutionContext, ValueTask<Type?>> value) => activity.Set(x => x.TargetType, value).WithReadContent();
        public static ISetupActivity<HttpRequestReceived> WithTargetType(this ISetupActivity<HttpRequestReceived> activity, Func<ValueTask<Type?>> value) => activity.Set(x => x.TargetType, value).WithReadContent();
        public static ISetupActivity<HttpRequestReceived> WithTargetType(this ISetupActivity<HttpRequestReceived> activity, Func<ActivityExecutionContext, Type?> value) => activity.Set(x => x.TargetType, value).WithReadContent();
        public static ISetupActivity<HttpRequestReceived> WithTargetType(this ISetupActivity<HttpRequestReceived> activity, Func<Type?> value) => activity.Set(x => x.TargetType, value).WithReadContent();
        public static ISetupActivity<HttpRequestReceived> WithTargetType(this ISetupActivity<HttpRequestReceived> activity, Type? value) => activity.Set(x => x.TargetType, value).WithReadContent();
        public static ISetupActivity<HttpRequestReceived> WithTargetType<T>(this ISetupActivity<HttpRequestReceived> activity) => activity.Set(x => x.TargetType, typeof(T)).WithReadContent();
    }
}