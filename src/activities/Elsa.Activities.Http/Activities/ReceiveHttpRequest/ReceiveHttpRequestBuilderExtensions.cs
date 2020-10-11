using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class ReceiveHttpRequestBuilderExtensions
    {
        public static IActivityBuilder ReceiveHttpRequest(
            this IBuilder builder,
            Action<ISetupActivity<ReceiveHttpRequest>> setup) => builder.Then(setup);

        public static IActivityBuilder ReceiveHttpRequest(
            this IBuilder builder,
            Action<ReceiveHttpRequest> setup) => builder.Then(setup);

        public static IActivityBuilder ReceiveHttpRequest(this IBuilder builder,
            Func<ActivityExecutionContext, ValueTask<PathString>> path) =>
            builder.ReceiveHttpRequest(setup => setup.Set(x => x.Path, path));

        public static IActivityBuilder ReceiveHttpRequest(this IBuilder builder,
            Func<ActivityExecutionContext, PathString> path) =>
            builder.ReceiveHttpRequest(setup => setup.Set(x => x.Path, path));

        public static IActivityBuilder ReceiveHttpRequest(this IBuilder builder, Func<ValueTask<PathString>> path) =>
            builder.ReceiveHttpRequest(setup => setup.Set(x => x.Path, path));

        public static IActivityBuilder ReceiveHttpRequest(this IBuilder builder, Func<PathString> path) =>
            builder.ReceiveHttpRequest(setup => setup.Set(x => x.Path, path));

        public static IActivityBuilder ReceiveHttpRequest(this IBuilder builder, PathString path) =>
            builder.ReceiveHttpRequest(x => x.Path = path);
    }
}