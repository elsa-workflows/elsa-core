using Elsa.Activities.Http.Models;
using Elsa.Builders;
using Elsa.Services.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elsa.Activities.Http
{
    public static class SendHttpRequestExtensions
    {
        public static ISetupActivity<SendHttpRequest> WithUrl(this ISetupActivity<SendHttpRequest> activity, Func<ActivityExecutionContext, ValueTask<Uri?>> url) => activity.Set(x => x.Url, url);

        public static ISetupActivity<SendHttpRequest> WithUrl(this ISetupActivity<SendHttpRequest> activity, Func<ValueTask<Uri?>> url) => activity.Set(x => x.Url, url);

        public static ISetupActivity<SendHttpRequest> WithUrl(this ISetupActivity<SendHttpRequest> activity, Func<ActivityExecutionContext, Uri?> url) => activity.Set(x => x.Url, url);

        public static ISetupActivity<SendHttpRequest> WithUrl(this ISetupActivity<SendHttpRequest> activity, Func<Uri?> url) => activity.Set(x => x.Url, url);

        public static ISetupActivity<SendHttpRequest> WithUrl(this ISetupActivity<SendHttpRequest> activity, Uri? url) => activity.Set(x => x.Url, url);


        public static ISetupActivity<SendHttpRequest> WithMethod(this ISetupActivity<SendHttpRequest> activity, Func<ActivityExecutionContext, ValueTask<string?>> method) => activity.Set(x => x.Method, method);

        public static ISetupActivity<SendHttpRequest> WithMethod(this ISetupActivity<SendHttpRequest> activity, Func<ValueTask<string?>> method) => activity.Set(x => x.Method, method);

        public static ISetupActivity<SendHttpRequest> WithMethod(this ISetupActivity<SendHttpRequest> activity, Func<ActivityExecutionContext, string?> method) => activity.Set(x => x.Method, method);

        public static ISetupActivity<SendHttpRequest> WithMethod(this ISetupActivity<SendHttpRequest> activity, Func<string?> method) => activity.Set(x => x.Method, method);

        public static ISetupActivity<SendHttpRequest> WithMethod(this ISetupActivity<SendHttpRequest> activity, string? method) => activity.Set(x => x.Method, method);


        public static ISetupActivity<SendHttpRequest> WithContent(this ISetupActivity<SendHttpRequest> activity, Func<ActivityExecutionContext, ValueTask<string?>> content
            ) => activity.Set(x => x.Content, content);

        public static ISetupActivity<SendHttpRequest> WithContent(this ISetupActivity<SendHttpRequest> activity, Func<ValueTask<string?>> content) => activity.Set(x => x.Content, content);

        public static ISetupActivity<SendHttpRequest> WithContent(this ISetupActivity<SendHttpRequest> activity, Func<ActivityExecutionContext, string?> content) => activity.Set(x => x.Content, content);

        public static ISetupActivity<SendHttpRequest> WithContent(this ISetupActivity<SendHttpRequest> activity, Func<string?> content) => activity.Set(x => x.Content, content);

        public static ISetupActivity<SendHttpRequest> WithContent(this ISetupActivity<SendHttpRequest> activity, string? content) => activity.Set(x => x.Content, content);


        public static ISetupActivity<SendHttpRequest> WithContentType(this ISetupActivity<SendHttpRequest> activity, Func<ActivityExecutionContext, ValueTask<string?>> contentType
            ) => activity.Set(x => x.ContentType, contentType);

        public static ISetupActivity<SendHttpRequest> WithContentType(this ISetupActivity<SendHttpRequest> activity, Func<ValueTask<string?>> contentType) => activity.Set(x => x.ContentType, contentType);

        public static ISetupActivity<SendHttpRequest> WithContentType(this ISetupActivity<SendHttpRequest> activity, Func<ActivityExecutionContext, string?> contentType) => activity.Set(x => x.ContentType, contentType);

        public static ISetupActivity<SendHttpRequest> WithContentType(this ISetupActivity<SendHttpRequest> activity, Func<string?> contentType) => activity.Set(x => x.ContentType, contentType);

        public static ISetupActivity<SendHttpRequest> WithContentType(this ISetupActivity<SendHttpRequest> activity, string? contentType) => activity.Set(x => x.ContentType, contentType);


        public static ISetupActivity<SendHttpRequest> WithAuthorization(this ISetupActivity<SendHttpRequest> activity, Func<ActivityExecutionContext, ValueTask<string?>> authorization
            ) => activity.Set(x => x.Authorization, authorization);

        public static ISetupActivity<SendHttpRequest> WithAuthorization(this ISetupActivity<SendHttpRequest> activity, Func<ValueTask<string?>> authorization) => activity.Set(x => x.Authorization, authorization);

        public static ISetupActivity<SendHttpRequest> WithAuthorization(this ISetupActivity<SendHttpRequest> activity, Func<ActivityExecutionContext, string?> authorization) => activity.Set(x => x.Authorization, authorization);

        public static ISetupActivity<SendHttpRequest> WithAuthorization(this ISetupActivity<SendHttpRequest> activity, Func<string?> authorization) => activity.Set(x => x.Authorization, authorization);

        public static ISetupActivity<SendHttpRequest> WithAuthorization(this ISetupActivity<SendHttpRequest> activity, string? authorization) => activity.Set(x => x.Authorization, authorization);


        public static ISetupActivity<SendHttpRequest> WithRequestHeaders(this ISetupActivity<SendHttpRequest> activity, Func<ActivityExecutionContext, ValueTask<HttpRequestHeaders?>> requestHeaders
            ) => activity.Set(x => x.RequestHeaders, requestHeaders);

        public static ISetupActivity<SendHttpRequest> WithRequestHeaders(this ISetupActivity<SendHttpRequest> activity, Func<ValueTask<HttpRequestHeaders?>> requestHeaders) => activity.Set(x => x.RequestHeaders, requestHeaders);

        public static ISetupActivity<SendHttpRequest> WithRequestHeaders(this ISetupActivity<SendHttpRequest> activity, Func<ActivityExecutionContext, HttpRequestHeaders?> requestHeaders) => activity.Set(x => x.RequestHeaders, requestHeaders);

        public static ISetupActivity<SendHttpRequest> WithRequestHeaders(this ISetupActivity<SendHttpRequest> activity, Func<HttpRequestHeaders?> requestHeaders) => activity.Set(x => x.RequestHeaders, requestHeaders);

        public static ISetupActivity<SendHttpRequest> WithRequestHeaders(this ISetupActivity<SendHttpRequest> activity, HttpRequestHeaders? requestHeaders) => activity.Set(x => x.RequestHeaders, requestHeaders);


        public static ISetupActivity<SendHttpRequest> WithReadContent(this ISetupActivity<SendHttpRequest> activity, Func<ActivityExecutionContext, ValueTask<bool?>> readContent
            ) => activity.Set(x => x.ReadContent, readContent);

        public static ISetupActivity<SendHttpRequest> WithReadContent(this ISetupActivity<SendHttpRequest> activity, Func<ValueTask<bool?>> readContent) => activity.Set(x => x.ReadContent, readContent);

        public static ISetupActivity<SendHttpRequest> WithReadContent(this ISetupActivity<SendHttpRequest> activity, Func<ActivityExecutionContext, bool?> readContent) => activity.Set(x => x.ReadContent, readContent);

        public static ISetupActivity<SendHttpRequest> WithReadContent(this ISetupActivity<SendHttpRequest> activity, Func<bool?> readContent) => activity.Set(x => x.ReadContent, readContent);

        public static ISetupActivity<SendHttpRequest> WithReadContent(this ISetupActivity<SendHttpRequest> activity, bool? readContent) => activity.Set(x => x.ReadContent, readContent);


        public static ISetupActivity<SendHttpRequest> WithSupportedHttpCodes(this ISetupActivity<SendHttpRequest> activity, Func<ActivityExecutionContext, ValueTask<ICollection<int>?>> supportedStatusCodes
            ) => activity.Set(x => x.SupportedStatusCodes, supportedStatusCodes);

        public static ISetupActivity<SendHttpRequest> WithSupportedHttpCodes(this ISetupActivity<SendHttpRequest> activity, Func<ValueTask<ICollection<int>?>> supportedStatusCodes) => activity.Set(x => x.SupportedStatusCodes, supportedStatusCodes);

        public static ISetupActivity<SendHttpRequest> WithSupportedHttpCodes(this ISetupActivity<SendHttpRequest> activity, Func<ActivityExecutionContext, ICollection<int>> supportedStatusCodes) => activity.Set(x => x.SupportedStatusCodes, supportedStatusCodes);

        public static ISetupActivity<SendHttpRequest> WithSupportedHttpCodes(this ISetupActivity<SendHttpRequest> activity, Func<ICollection<int>> supportedStatusCodes) => activity.Set(x => x.SupportedStatusCodes, supportedStatusCodes);

        public static ISetupActivity<SendHttpRequest> WithSupportedHttpCodes(this ISetupActivity<SendHttpRequest> activity, ICollection<int> supportedStatusCodes) => activity.Set(x => x.SupportedStatusCodes, supportedStatusCodes);
    }
}
