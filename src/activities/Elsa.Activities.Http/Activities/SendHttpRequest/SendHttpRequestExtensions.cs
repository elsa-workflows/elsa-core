using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class SendHttpRequestExtensions
    {
        /// <summary>
        /// Specify the URL of the HTTP Request.
        /// </summary>
        /// <param name="url">The address of the resource.</param>
        /// <returns>A <see cref="ISetupActivity"/> that can be used to further change properties of <see cref="SendHttpRequest"/></returns>
        public static ISetupActivity<Http.SendHttpRequest> WithUrl(this ISetupActivity<Http.SendHttpRequest> activity, Func<ActivityExecutionContext, ValueTask<Uri?>> url) => activity.Set(x => x.Url, url);

        /// <inheritdoc cref="WithUrl(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{Uri?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithUrl(this ISetupActivity<Http.SendHttpRequest> activity, Func<ValueTask<Uri?>> url) => activity.Set(x => x.Url, url);

        /// <inheritdoc cref="WithUrl(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{Uri?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithUrl(this ISetupActivity<Http.SendHttpRequest> activity, Func<ActivityExecutionContext, Uri?> url) => activity.Set(x => x.Url, url);

        /// <inheritdoc cref="WithUrl(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{Uri?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithUrl(this ISetupActivity<Http.SendHttpRequest> activity, Func<Uri?> url) => activity.Set(x => x.Url, url);

        /// <inheritdoc cref="WithUrl(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{Uri?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithUrl(this ISetupActivity<Http.SendHttpRequest> activity, Uri? url) => activity.Set(x => x.Url, url);


        /// <summary>
        /// Specify the method to use with the HTTP request.
        /// </summary>
        /// <param name="method">Set the HTTP method to use with the HTTP Request</param>
        /// <returns>A <see cref="ISetupActivity"/> that can be used to further change properties of <see cref="SendHttpRequest"/></returns>
        public static ISetupActivity<Http.SendHttpRequest> WithMethod(this ISetupActivity<Http.SendHttpRequest> activity, Func<ActivityExecutionContext, ValueTask<string?>> method) => activity.Set(x => x.Method, method);

        /// <inheritdoc cref="WithMethod(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{string?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithMethod(this ISetupActivity<Http.SendHttpRequest> activity, Func<ValueTask<string?>> method) => activity.Set(x => x.Method, method);

        /// <inheritdoc cref="WithMethod(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{string?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithMethod(this ISetupActivity<Http.SendHttpRequest> activity, Func<ActivityExecutionContext, string?> method) => activity.Set(x => x.Method, method);

        /// <inheritdoc cref="WithMethod(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{string?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithMethod(this ISetupActivity<Http.SendHttpRequest> activity, Func<string?> method) => activity.Set(x => x.Method, method);

        /// <inheritdoc cref="WithMethod(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{string?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithMethod(this ISetupActivity<Http.SendHttpRequest> activity, string? method) => activity.Set(x => x.Method, method);


        /// <summary>
        /// Specify the content to send with the HTTP request.
        /// </summary>
        /// <param name="content">The content of the HTTP request</param>
        /// <returns>A <see cref="ISetupActivity"/> that can be used to further change properties of <see cref="SendHttpRequest"/></returns>
        public static ISetupActivity<Http.SendHttpRequest> WithContent(this ISetupActivity<Http.SendHttpRequest> activity, Func<ActivityExecutionContext, ValueTask<string?>> content
            ) => activity.Set(x => x.Content, content);

        /// <inheritdoc cref="WithContent(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{string?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithContent(this ISetupActivity<Http.SendHttpRequest> activity, Func<ValueTask<string?>> content) => activity.Set(x => x.Content, content);

        /// <inheritdoc cref="WithContent(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{string?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithContent(this ISetupActivity<Http.SendHttpRequest> activity, Func<ActivityExecutionContext, string?> content) => activity.Set(x => x.Content, content);

        /// <inheritdoc cref="WithContent(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{string?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithContent(this ISetupActivity<Http.SendHttpRequest> activity, Func<string?> content) => activity.Set(x => x.Content, content);

        /// <inheritdoc cref="WithContent(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{string?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithContent(this ISetupActivity<Http.SendHttpRequest> activity, string? content) => activity.Set(x => x.Content, content);


        /// <summary>
        /// Specify the Content-Type header of the HTTP request.
        /// </summary>
        /// <param name="contentType">The value to set the Content-Type header to</param>
        /// <returns>A <see cref="ISetupActivity"/> that can be used to further change properties of <see cref="SendHttpRequest"/></returns>
        public static ISetupActivity<Http.SendHttpRequest> WithContentType(this ISetupActivity<Http.SendHttpRequest> activity, Func<ActivityExecutionContext, ValueTask<string?>> contentType
            ) => activity.Set(x => x.ContentType, contentType);

        /// <inheritdoc cref="WithContentType(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{string?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithContentType(this ISetupActivity<Http.SendHttpRequest> activity, Func<ValueTask<string?>> contentType) => activity.Set(x => x.ContentType, contentType);

        /// <inheritdoc cref="WithContentType(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{string?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithContentType(this ISetupActivity<Http.SendHttpRequest> activity, Func<ActivityExecutionContext, string?> contentType) => activity.Set(x => x.ContentType, contentType);

        /// <inheritdoc cref="WithContentType(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{string?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithContentType(this ISetupActivity<Http.SendHttpRequest> activity, Func<string?> contentType) => activity.Set(x => x.ContentType, contentType);

        /// <inheritdoc cref="WithContentType(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{string?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithContentType(this ISetupActivity<Http.SendHttpRequest> activity, string? contentType) => activity.Set(x => x.ContentType, contentType);


        /// <summary>
        /// Specify the Authorization header of the HTTP request.
        /// </summary>
        /// <param name="authorization">The value of the Authorization header</param>
        /// <returns>A <see cref="ISetupActivity"/> that can be used to further change properties of <see cref="SendHttpRequest"/></returns>
        public static ISetupActivity<Http.SendHttpRequest> WithAuthorization(this ISetupActivity<Http.SendHttpRequest> activity, Func<ActivityExecutionContext, ValueTask<string?>> authorization
            ) => activity.Set(x => x.Authorization, authorization);

        /// <inheritdoc cref="WithAuthorization(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{string?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithAuthorization(this ISetupActivity<Http.SendHttpRequest> activity, Func<ValueTask<string?>> authorization) => activity.Set(x => x.Authorization, authorization);

        /// <inheritdoc cref="WithAuthorization(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{string?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithAuthorization(this ISetupActivity<Http.SendHttpRequest> activity, Func<ActivityExecutionContext, string?> authorization) => activity.Set(x => x.Authorization, authorization);

        /// <inheritdoc cref="WithAuthorization(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{string?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithAuthorization(this ISetupActivity<Http.SendHttpRequest> activity, Func<string?> authorization) => activity.Set(x => x.Authorization, authorization);

        /// <inheritdoc cref="WithAuthorization(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{string?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithAuthorization(this ISetupActivity<Http.SendHttpRequest> activity, string? authorization) => activity.Set(x => x.Authorization, authorization);

        /// <summary>
        /// Set additional headers for the HTTP request.
        /// </summary>
        /// <param name="requestHeaders">A <see cref="Dictionary"/> of additional headers to send with the request.</param>
        /// <returns>A <see cref="ISetupActivity"/> that can be used to further change properties of <see cref="SendHttpRequest"/></returns>
        public static ISetupActivity<Http.SendHttpRequest> WithRequestHeaders(this ISetupActivity<Http.SendHttpRequest> activity, Func<ActivityExecutionContext, ValueTask<HttpRequestHeaders?>> requestHeaders
            ) => activity.Set(x => x.RequestHeaders, requestHeaders);

        /// <inheritdoc cref="WithRequestHeaders(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{HttpRequestHeaders?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithRequestHeaders(this ISetupActivity<Http.SendHttpRequest> activity, Func<ValueTask<HttpRequestHeaders?>> requestHeaders) => activity.Set(x => x.RequestHeaders, requestHeaders);

        /// <inheritdoc cref="WithRequestHeaders(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{HttpRequestHeaders?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithRequestHeaders(this ISetupActivity<Http.SendHttpRequest> activity, Func<ActivityExecutionContext, HttpRequestHeaders?> requestHeaders) => activity.Set(x => x.RequestHeaders, requestHeaders);

        /// <inheritdoc cref="WithRequestHeaders(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{HttpRequestHeaders?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithRequestHeaders(this ISetupActivity<Http.SendHttpRequest> activity, Func<HttpRequestHeaders?> requestHeaders) => activity.Set(x => x.RequestHeaders, requestHeaders);

        /// <inheritdoc cref="WithRequestHeaders(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{HttpRequestHeaders?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithRequestHeaders(this ISetupActivity<Http.SendHttpRequest> activity, HttpRequestHeaders? requestHeaders) => activity.Set(x => x.RequestHeaders, requestHeaders);


        /// <summary>
        /// Sets whether or not to read the content of the HTTP response.
        /// </summary>
        /// <param name="readContent">Whether or not to read the content of the HTTP response.</param>
        /// <returns>A <see cref="ISetupActivity"/> that can be used to further change properties of <see cref="SendHttpRequest"/></returns>
        public static ISetupActivity<Http.SendHttpRequest> WithReadContent(this ISetupActivity<Http.SendHttpRequest> activity, Func<ActivityExecutionContext, ValueTask<bool?>> readContent
            ) => activity.Set(x => x.ReadContent, readContent);

        /// <inheritdoc cref="WithReadContent(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{bool?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithReadContent(this ISetupActivity<Http.SendHttpRequest> activity, Func<ValueTask<bool?>> readContent) => activity.Set(x => x.ReadContent, readContent);

        /// <inheritdoc cref="WithReadContent(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{bool?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithReadContent(this ISetupActivity<Http.SendHttpRequest> activity, Func<ActivityExecutionContext, bool?> readContent) => activity.Set(x => x.ReadContent, readContent);

        /// <inheritdoc cref="WithReadContent(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{bool?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithReadContent(this ISetupActivity<Http.SendHttpRequest> activity, Func<bool?> readContent) => activity.Set(x => x.ReadContent, readContent);

        /// <inheritdoc cref="WithReadContent(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{bool?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithReadContent(this ISetupActivity<Http.SendHttpRequest> activity, bool? readContent) => activity.Set(x => x.ReadContent, readContent);


        /// <summary>
        /// Sets the response status codes that are supported. Can be used to handle different outcomes
        /// </summary>
        /// <param name="supportedStatusCodes">The list of supported status codes</param>
        /// <returns>A <see cref="ISetupActivity"/> that can be used to further change properties of <see cref="SendHttpRequest"/></returns>
        public static ISetupActivity<Http.SendHttpRequest> WithSupportedHttpCodes(this ISetupActivity<Http.SendHttpRequest> activity, Func<ActivityExecutionContext, ValueTask<ICollection<int>?>> supportedStatusCodes
            ) => activity.Set(x => x.SupportedStatusCodes, supportedStatusCodes);

        /// <inheritdoc cref="WithSupportedHttpCodes(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{ICollection{int}?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithSupportedHttpCodes(this ISetupActivity<Http.SendHttpRequest> activity, Func<ValueTask<ICollection<int>?>> supportedStatusCodes) => activity.Set(x => x.SupportedStatusCodes, supportedStatusCodes);

        /// <inheritdoc cref="WithSupportedHttpCodes(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{ICollection{int}?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithSupportedHttpCodes(this ISetupActivity<Http.SendHttpRequest> activity, Func<ActivityExecutionContext, ICollection<int>> supportedStatusCodes) => activity.Set(x => x.SupportedStatusCodes, supportedStatusCodes);

        /// <inheritdoc cref="WithSupportedHttpCodes(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{ICollection{int}?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithSupportedHttpCodes(this ISetupActivity<Http.SendHttpRequest> activity, Func<ICollection<int>> supportedStatusCodes) => activity.Set(x => x.SupportedStatusCodes, supportedStatusCodes);

        /// <inheritdoc cref="WithSupportedHttpCodes(ISetupActivity{SendHttpRequest}, Func{ActivityExecutionContext, ValueTask{ICollection{int}?}})"/>
        public static ISetupActivity<Http.SendHttpRequest> WithSupportedHttpCodes(this ISetupActivity<Http.SendHttpRequest> activity, ICollection<int> supportedStatusCodes) => activity.Set(x => x.SupportedStatusCodes, supportedStatusCodes);
    }
}
