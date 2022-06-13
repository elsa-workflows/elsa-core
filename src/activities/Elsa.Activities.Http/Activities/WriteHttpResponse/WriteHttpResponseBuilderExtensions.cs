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
        /// <summary>
        /// Creates a <see cref="Http.WriteHttpResponse"/> activity.
        /// </summary>
        /// <param name="setup">An <see cref="ISetupActivity"/> used to set the properties of the <see cref="Http.WriteHttpResponse"/> Activity.</param>
        /// <returns>The <see cref="IActivityBuilder"/> with the Write HTTP Response activity built onto it.</returns>
        /// <inheritdoc cref="BuilderExtensions.Then{T}(IBuilder, Action{ISetupActivity{T}}?, Action{IActivityBuilder}?, int, string?)"/>
        public static IActivityBuilder WriteHttpResponse(this IBuilder builder, Action<ISetupActivity<WriteHttpResponse>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        /// <param name="statusCode">The <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Status">status code</see> that is returned with the response.</param>
        /// <param name="content">The content to send along with the response.</param>
        /// <param name="contentType">The Content-Type header to send along with the response.</param>
        /// <inheritdoc cref="WriteHttpResponse(IBuilder, Action{ISetupActivity{WriteHttpResponse}}, int, string?)"/>
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

        /// <inheritdoc cref="WriteHttpResponse(IBuilder, Func{ActivityExecutionContext, HttpStatusCode}, Func{ActivityExecutionContext, ValueTask{string?}}, Func{ActivityExecutionContext, string?}, int, string?)"/>
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

        /// <inheritdoc cref="WriteHttpResponse(IBuilder, Func{ActivityExecutionContext, HttpStatusCode}, Func{ActivityExecutionContext, ValueTask{string?}}, Func{ActivityExecutionContext, string?}, int, string?)"/>
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

        /// <inheritdoc cref="WriteHttpResponse(IBuilder, Func{ActivityExecutionContext, HttpStatusCode}, Func{ActivityExecutionContext, ValueTask{string?}}, Func{ActivityExecutionContext, string?}, int, string?)"/>
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