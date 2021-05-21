using Elsa.Activities.Http.Models;
using Elsa.Builders;
using Elsa.Services.Models;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Elsa.Activities.Http
{
    public static class SendHttpRequestBuilderExtensions
    {
        public static IActivityBuilder SendHttpRequest(this IActivityBuilder builder, Action<ISetupActivity<SendHttpRequest>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) => 
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder SendHttpRequest(this IActivityBuilder builder,
            Func<ActivityExecutionContext, Uri?> url,
            Func<ActivityExecutionContext, string?> method,
            Func<ActivityExecutionContext, string?> content,
            Func<ActivityExecutionContext, string?> contentType,
            Func<ActivityExecutionContext, string?> authorization,
            Func<ActivityExecutionContext, HttpRequestHeaders> requestHeaders,
            Func<ActivityExecutionContext, bool> readContent,
            Func<ActivityExecutionContext, ICollection<int>> supportedStatusCodes,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => 
            builder.SendHttpRequest(
                setup => setup
                    .Set(x => x.Url, url)
                    .Set(x => x.Method, method)
                    .Set(x => x.Content, content)
                    .Set(x => x.ContentType, contentType)
                    .Set(x => x.Authorization, authorization)
                    .Set(x => x.RequestHeaders, requestHeaders)
                    .Set(x => x.ReadContent, readContent)
                    .Set(x => x.SupportedStatusCodes, supportedStatusCodes),
                lineNumber,
                sourceFile);

        public static IActivityBuilder SendHttpRequest(this IActivityBuilder builder,
            Func<Uri?> url,
            Func<string?> method,
            Func<string?> content,
            Func<string?> contentType,
            Func<string?> authorization,
            Func<HttpRequestHeaders> requestHeaders,
            Func<bool> readContent,
            Func<ICollection<int>> supportedStatusCodes,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.SendHttpRequest(
                setup => setup
                    .Set(x => x.Url, url)
                    .Set(x => x.Method, method)
                    .Set(x => x.Content, content)
                    .Set(x => x.ContentType, contentType)
                    .Set(x => x.Authorization, authorization)
                    .Set(x => x.RequestHeaders, requestHeaders)
                    .Set(x => x.ReadContent, readContent)
                    .Set(x => x.SupportedStatusCodes, supportedStatusCodes),
                lineNumber,
                sourceFile);

        public static IActivityBuilder SendHttpRequest(this IActivityBuilder builder,
            Uri? url,
            string? method,
            string? content,
            string? contentType,
            string? authorization,
            HttpRequestHeaders requestHeaders,
            bool readContent,
            ICollection<int> supportedStatusCodes,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.SendHttpRequest(
                setup => setup
                    .Set(x => x.Url, url)
                    .Set(x => x.Method, method)
                    .Set(x => x.Content, content)
                    .Set(x => x.ContentType, contentType)
                    .Set(x => x.Authorization, authorization)
                    .Set(x => x.RequestHeaders, requestHeaders)
                    .Set(x => x.ReadContent, readContent)
                    .Set(x => x.SupportedStatusCodes, supportedStatusCodes),
                lineNumber,
                sourceFile);
    }
}
