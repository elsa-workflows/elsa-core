using System;
using System.Net;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.Builders;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class WriteHttpResponseBuilderExtensions
    {
        public static IActivityBuilder WriteHttpResponse(this IBuilder builder,
            Action<ISetupActivity<WriteHttpResponse>> setup) => builder.Then(setup);

        public static IActivityBuilder WriteHttpResponse(
            this IBuilder builder,
            Func<ActivityExecutionContext, HttpStatusCode> statusCode,
            Func<ActivityExecutionContext, ValueTask<string>> content,
            Func<ActivityExecutionContext, string> contentType) =>
            builder.WriteHttpResponse(
                setup => setup
                    .Set(x => x.StatusCode, statusCode)
                    .Set(x => x.Content, content)
                    .Set(x => x.ContentType, contentType));

        public static IActivityBuilder WriteHttpResponse(
            this IBuilder builder,
            Func<ActivityExecutionContext, HttpStatusCode> statusCode,
            Func<ActivityExecutionContext, string> content,
            Func<ActivityExecutionContext, string> contentType) =>
            builder.WriteHttpResponse(
                setup => setup
                    .Set(x => x.StatusCode, statusCode)
                    .Set(x => x.Content, content)
                    .Set(x => x.ContentType, contentType));

        public static IActivityBuilder WriteHttpResponse(
            this IBuilder builder,
            Func<HttpStatusCode> statusCode,
            Func<string> content,
            Func<string> contentType) =>
            builder.WriteHttpResponse(
                setup => setup
                    .Set(x => x.StatusCode, statusCode)
                    .Set(x => x.Content, content)
                    .Set(x => x.ContentType, contentType));

        public static IActivityBuilder WriteHttpResponse(
            this IBuilder builder,
            HttpStatusCode statusCode,
            string content,
            string contentType) =>
            builder.WriteHttpResponse(
                setup => setup
                    .Set(x => x.StatusCode, statusCode)
                    .Set(x => x.Content, content)
                    .Set(x => x.ContentType, contentType));
    }
}