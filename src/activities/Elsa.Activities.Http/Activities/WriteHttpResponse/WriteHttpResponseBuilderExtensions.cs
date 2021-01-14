using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class WriteHttpResponseBuilderExtensions
    {
        public static IActivityBuilder WriteHttpResponse(this IBuilder builder, Action<ISetupActivity<WriteHttpResponse>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder WriteHttpResponse(
            this IBuilder builder,
            Func<ActivityExecutionContext, HttpStatusCode> statusCode,
            Func<ActivityExecutionContext, ValueTask<string?>> content,
            Func<ActivityExecutionContext, string?> contentType,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.WriteHttpResponse(
                setup => setup
                    .Set(x => x.StatusCode, statusCode)
                    .Set(x => x.Content, content)
                    .Set(x => x.ContentType, contentType),
                lineNumber,
                sourceFile);

        public static IActivityBuilder WriteHttpResponse(
            this IBuilder builder,
            Func<ActivityExecutionContext, HttpStatusCode> statusCode,
            Func<ActivityExecutionContext, string?> content,
            Func<ActivityExecutionContext, string?> contentType,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.WriteHttpResponse(
                setup => setup
                    .Set(x => x.StatusCode, statusCode)
                    .Set(x => x.Content, content)
                    .Set(x => x.ContentType, contentType),
                lineNumber,
                sourceFile);

        public static IActivityBuilder WriteHttpResponse(
            this IBuilder builder,
            Func<HttpStatusCode> statusCode,
            Func<string?> content,
            Func<string?> contentType,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.WriteHttpResponse(
                setup => setup
                    .Set(x => x.StatusCode, statusCode)
                    .Set(x => x.Content, content)
                    .Set(x => x.ContentType, contentType),
                lineNumber,
                sourceFile);

        public static IActivityBuilder WriteHttpResponse(
            this IBuilder builder,
            HttpStatusCode statusCode,
            string? content,
            string? contentType,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.WriteHttpResponse(
                setup => setup
                    .Set(x => x.StatusCode, statusCode)
                    .Set(x => x.Content, content)
                    .Set(x => x.ContentType, contentType),
                lineNumber,
                sourceFile);
    }
}