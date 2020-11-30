using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class HttpRequestReceivedBuilderExtensions
    {
        public static IActivityBuilder HttpRequestReceived(this IBuilder builder, Action<ISetupActivity<HttpRequestReceived>> setup) => builder.Then(setup);
        public static IActivityBuilder HttpRequestReceived(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<PathString>> path) => builder.HttpRequestReceived(setup => setup.Set(x => x.Path, path));
        public static IActivityBuilder HttpRequestReceived(this IBuilder builder, Func<ActivityExecutionContext, PathString> path) => builder.HttpRequestReceived(setup => setup.Set(x => x.Path, path));
        public static IActivityBuilder HttpRequestReceived(this IBuilder builder, Func<ValueTask<PathString>> path) => builder.HttpRequestReceived(setup => setup.Set(x => x.Path, path));
        public static IActivityBuilder HttpRequestReceived(this IBuilder builder, Func<PathString> path) => builder.HttpRequestReceived(setup => setup.Set(x => x.Path, path));
        public static IActivityBuilder HttpRequestReceived(this IBuilder builder, PathString path) => builder.HttpRequestReceived(setup => setup.Set(x => x.Path, path));
        public static IActivityBuilder ReceiveHttpPostRequest<T>(this IBuilder builder, Func<ActivityExecutionContext, PathString> path) => builder.HttpRequestReceived(activity => activity.WithPath(path).WithMethod(HttpMethods.Post).WithTargetType<T>());
        public static IActivityBuilder ReceiveHttpPostRequest<T>(this IBuilder builder, Func<PathString> path) => builder.HttpRequestReceived(activity => activity.WithPath(path).WithMethod(HttpMethods.Post).WithTargetType<T>());
        public static IActivityBuilder ReceiveHttpPostRequest<T>(this IBuilder builder, PathString path) => builder.HttpRequestReceived(activity => activity.WithPath(path).WithMethod(HttpMethods.Post).WithTargetType<T>());
    }
}