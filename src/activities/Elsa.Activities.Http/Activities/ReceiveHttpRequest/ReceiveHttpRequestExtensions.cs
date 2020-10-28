using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class ReceiveHttpRequestExtensions
    {
        public static ISetupActivity<ReceiveHttpRequest> WithPath(this ISetupActivity<ReceiveHttpRequest> activity, Func<ActivityExecutionContext, ValueTask<PathString>> path) => activity.Set(x => x.Path, path);
        public static ISetupActivity<ReceiveHttpRequest> WithPath(this ISetupActivity<ReceiveHttpRequest> activity, Func<ValueTask<PathString>> path) => activity.Set(x => x.Path, path);
        public static ISetupActivity<ReceiveHttpRequest> WithPath(this ISetupActivity<ReceiveHttpRequest> activity, Func<ActivityExecutionContext, PathString> path) => activity.Set(x => x.Path, path);
        public static ISetupActivity<ReceiveHttpRequest> WithPath(this ISetupActivity<ReceiveHttpRequest> activity, Func<PathString> path) => activity.Set(x => x.Path, path);
        public static ISetupActivity<ReceiveHttpRequest> WithPath(this ISetupActivity<ReceiveHttpRequest> activity, PathString path) => activity.Set(x => x.Path, path);

        public static ISetupActivity<ReceiveHttpRequest> WithMethod(this ISetupActivity<ReceiveHttpRequest> activity, Func<ActivityExecutionContext, ValueTask<string?>> method) => activity.Set(x => x.Method, method);
        public static ISetupActivity<ReceiveHttpRequest> WithMethod(this ISetupActivity<ReceiveHttpRequest> activity, Func<ValueTask<string?>> method) => activity.Set(x => x.Method, method);
        public static ISetupActivity<ReceiveHttpRequest> WithMethod(this ISetupActivity<ReceiveHttpRequest> activity, Func<ActivityExecutionContext, string?> method) => activity.Set(x => x.Method, method);
        public static ISetupActivity<ReceiveHttpRequest> WithMethod(this ISetupActivity<ReceiveHttpRequest> activity, Func<string?> method) => activity.Set(x => x.Method, method);
        public static ISetupActivity<ReceiveHttpRequest> WithMethod(this ISetupActivity<ReceiveHttpRequest> activity, string? method) => activity.Set(x => x.Method, method);

        public static ISetupActivity<ReceiveHttpRequest> WithReadContent(this ISetupActivity<ReceiveHttpRequest> activity, Func<ActivityExecutionContext, ValueTask<bool>> value) => activity.Set(x => x.ReadContent, value);
        public static ISetupActivity<ReceiveHttpRequest> WithReadContent(this ISetupActivity<ReceiveHttpRequest> activity, Func<ValueTask<bool>> value) => activity.Set(x => x.ReadContent, value);
        public static ISetupActivity<ReceiveHttpRequest> WithReadContent(this ISetupActivity<ReceiveHttpRequest> activity, Func<ActivityExecutionContext, bool> value) => activity.Set(x => x.ReadContent, value);
        public static ISetupActivity<ReceiveHttpRequest> WithReadContent(this ISetupActivity<ReceiveHttpRequest> activity, Func<bool> value) => activity.Set(x => x.ReadContent, value);
        public static ISetupActivity<ReceiveHttpRequest> WithReadContent(this ISetupActivity<ReceiveHttpRequest> activity, bool value = true) => activity.Set(x => x.ReadContent, value);

        public static ISetupActivity<ReceiveHttpRequest> WithTargetType(this ISetupActivity<ReceiveHttpRequest> activity, Func<ActivityExecutionContext, ValueTask<Type?>> value) => activity.Set(x => x.TargetType, value).WithReadContent();
        public static ISetupActivity<ReceiveHttpRequest> WithTargetType(this ISetupActivity<ReceiveHttpRequest> activity, Func<ValueTask<Type?>> value) => activity.Set(x => x.TargetType, value).WithReadContent();
        public static ISetupActivity<ReceiveHttpRequest> WithTargetType(this ISetupActivity<ReceiveHttpRequest> activity, Func<ActivityExecutionContext, Type?> value) => activity.Set(x => x.TargetType, value).WithReadContent();
        public static ISetupActivity<ReceiveHttpRequest> WithTargetType(this ISetupActivity<ReceiveHttpRequest> activity, Func<Type?> value) => activity.Set(x => x.TargetType, value).WithReadContent();
        public static ISetupActivity<ReceiveHttpRequest> WithTargetType(this ISetupActivity<ReceiveHttpRequest> activity, Type? value) => activity.Set(x => x.TargetType, value).WithReadContent();
        public static ISetupActivity<ReceiveHttpRequest> WithTargetType<T>(this ISetupActivity<ReceiveHttpRequest> activity) => activity.Set(x => x.TargetType, typeof(T)).WithReadContent();
    }
}