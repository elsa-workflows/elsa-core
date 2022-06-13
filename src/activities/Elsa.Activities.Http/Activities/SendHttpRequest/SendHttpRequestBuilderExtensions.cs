using Elsa.Activities.Http.Models;
using Elsa.Builders;
using Elsa.Services.Models;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http;

public static class SendHttpRequestBuilderExtensions
{
    /// <summary>
    /// Creates an activity that sends an HTTP Request
    /// </summary>
    /// <param name="setup">Sets the properties of the SendHttpRequest activity</param>
    /// <returns>The <see cref="IActivityBuilder"/> with the <see cref="Http.SendHttpRequest"/> activity built onto it.</returns>
    /// <inheritdoc cref="BuilderExtensions.Then{T}(IBuilder, Action{ISetupActivity{T}}?, Action{IActivityBuilder}?, int, string?)"/>
    public static IActivityBuilder SendHttpRequest(
        this IActivityBuilder builder,
        Action<ISetupActivity<SendHttpRequest>> setup,
        Action<IActivityBuilder>? activity = default,
        [CallerLineNumber] int lineNumber = default,
        [CallerFilePath] string? sourceFile = default) =>
        builder.Then(setup, activity, lineNumber, sourceFile);

    /// <param name="url">Address of the resource.</param>
    /// <param name="method">The <see href="https://datatracker.ietf.org/doc/html/rfc7231#section-4.3">HTTP Verb</see> to use for the request.</param>
    /// <param name="content">The request body.</param>
    /// <param name="contentType">The Content-Type header of the request.</param>
    /// <param name="authorization">The Authorization header value of the request.</param>
    /// <param name="requestHeaders">Additional headers of the request.</param>
    /// <param name="readContent">Whether or not to read the content of the response.</param>
    /// <param name="supportedStatusCodes">List of <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Status">status codes</see> that are supported.</param>
    /// <inheritdoc cref="SendHttpRequest(IActivityBuilder, Action{ISetupActivity{SendHttpRequest}}, Action{IActivityBuilder}, int, string?)"/>
    public static IActivityBuilder SendHttpRequest(
        this IActivityBuilder builder,
        Func<ActivityExecutionContext, Uri?> url,
        Func<ActivityExecutionContext, string?> method,
        Func<ActivityExecutionContext, string?> content,
        Func<ActivityExecutionContext, string?> contentType,
        Func<ActivityExecutionContext, string?> authorization,
        Func<ActivityExecutionContext, HttpRequestHeaders> requestHeaders,
        Func<ActivityExecutionContext, bool> readContent,
        Func<ActivityExecutionContext, ICollection<int>> supportedStatusCodes,
        Action<IActivityBuilder>? activity = default,
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
            activity,
            lineNumber,
            sourceFile);

    /// <inheritdoc cref="SendHttpRequest(IActivityBuilder, Func{ActivityExecutionContext, Uri?}, Func{ActivityExecutionContext, string?}, Func{ActivityExecutionContext, string?}, Func{ActivityExecutionContext, string?}, Func{ActivityExecutionContext, string?}, Func{ActivityExecutionContext, HttpRequestHeaders}, Func{ActivityExecutionContext, bool}, Func{ActivityExecutionContext, ICollection{int}}, Action{IActivityBuilder}?, int, string?)"/>
    public static IActivityBuilder SendHttpRequest(
        this IActivityBuilder builder,
        Func<Uri?> url,
        Func<string?> method,
        Func<string?> content,
        Func<string?> contentType,
        Func<string?> authorization,
        Func<HttpRequestHeaders> requestHeaders,
        Func<bool> readContent,
        Func<ICollection<int>> supportedStatusCodes,
        Action<IActivityBuilder>? activity = default,
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
            activity,
            lineNumber,
            sourceFile);

    /// <inheritdoc cref="SendHttpRequest(IActivityBuilder, Func{ActivityExecutionContext, Uri?}, Func{ActivityExecutionContext, string?}, Func{ActivityExecutionContext, string?}, Func{ActivityExecutionContext, string?}, Func{ActivityExecutionContext, string?}, Func{ActivityExecutionContext, HttpRequestHeaders}, Func{ActivityExecutionContext, bool}, Func{ActivityExecutionContext, ICollection{int}}, Action{IActivityBuilder}?, int, string?)"/>
    public static IActivityBuilder SendHttpRequest(
        this IActivityBuilder builder,
        Uri? url,
        string? method,
        string? content,
        string? contentType,
        string? authorization,
        HttpRequestHeaders requestHeaders,
        bool readContent,
        ICollection<int> supportedStatusCodes,
        Action<IActivityBuilder>? activity = default,
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
            activity,
            lineNumber,
            sourceFile);
}