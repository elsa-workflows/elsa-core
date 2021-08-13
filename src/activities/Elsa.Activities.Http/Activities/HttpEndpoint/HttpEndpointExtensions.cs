using System;
using System.Collections.Generic;
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

        public static ISetupActivity<HttpEndpoint> WithMethods(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, ValueTask<IEnumerable<string>>> value) =>
            activity.Set(x => x.Methods, async context => new HashSet<string>(await value(context)));

        public static ISetupActivity<HttpEndpoint> WithMethods(this ISetupActivity<HttpEndpoint> activity, Func<ValueTask<IEnumerable<string>>> value) => activity.Set(x => x.Methods, async () => await value());

        public static ISetupActivity<HttpEndpoint> WithMethods(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, IEnumerable<string>> value) =>
            activity.Set(x => x.Methods, context => new HashSet<string>(value(context)));

        public static ISetupActivity<HttpEndpoint> WithMethods(this ISetupActivity<HttpEndpoint> activity, Func<IEnumerable<string>> value) => activity.Set(x => x.Methods, () => new HashSet<string>(value()));
        public static ISetupActivity<HttpEndpoint> WithMethods(this ISetupActivity<HttpEndpoint> activity, IEnumerable<string> value) => activity.Set(x => x.Methods, new HashSet<string>(value));

        public static ISetupActivity<HttpEndpoint> WithMethod(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, ValueTask<string>> value) => activity.WithMethods(async context => new[] { await value(context) });
        public static ISetupActivity<HttpEndpoint> WithMethod(this ISetupActivity<HttpEndpoint> activity, Func<ValueTask<string>> value) => activity.WithMethods(async () => new[] { await value() });
        public static ISetupActivity<HttpEndpoint> WithMethod(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, string> value) => activity.WithMethods(context => new[] { value(context) });
        public static ISetupActivity<HttpEndpoint> WithMethod(this ISetupActivity<HttpEndpoint> activity, Func<string> value) => activity.WithMethods(() => new[] { value() });
        public static ISetupActivity<HttpEndpoint> WithMethod(this ISetupActivity<HttpEndpoint> activity, string value) => activity.WithMethods(new[] { value });

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
        
        public static ISetupActivity<HttpEndpoint> WithAuthorize(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, ValueTask<bool>> value) => activity.Set(x => x.Authorize, value);
        public static ISetupActivity<HttpEndpoint> WithAuthorize(this ISetupActivity<HttpEndpoint> activity, Func<ValueTask<bool>> value) => activity.Set(x => x.Authorize, value);
        public static ISetupActivity<HttpEndpoint> WithAuthorize(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, bool> value) => activity.Set(x => x.Authorize, value);
        public static ISetupActivity<HttpEndpoint> WithAuthorize(this ISetupActivity<HttpEndpoint> activity, Func<bool> value) => activity.Set(x => x.Authorize, value);
        public static ISetupActivity<HttpEndpoint> WithAuthorize(this ISetupActivity<HttpEndpoint> activity, bool value = true) => activity.Set(x => x.Authorize, value);
        
        public static ISetupActivity<HttpEndpoint> WithPolicy(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, ValueTask<string?>> value) => activity.Set(x => x.Policy, value);
        public static ISetupActivity<HttpEndpoint> WithPolicy(this ISetupActivity<HttpEndpoint> activity, Func<ValueTask<string?>> value) => activity.Set(x => x.Policy, value);
        public static ISetupActivity<HttpEndpoint> WithPolicy(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, string?> value) => activity.Set(x => x.Policy, value);
        public static ISetupActivity<HttpEndpoint> WithPolicy(this ISetupActivity<HttpEndpoint> activity, Func<string?> value) => activity.Set(x => x.Policy, value);
        public static ISetupActivity<HttpEndpoint> WithPolicy(this ISetupActivity<HttpEndpoint> activity, string? value) => activity.Set(x => x.Policy, value);
    }
}