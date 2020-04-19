using System;
using System.Collections.Generic;
using Elsa.Activities.Http.Models;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class SendHttpRequestBuilderExtensions
    {
        public static IActivityBuilder SendHttpRequest(
            this IBuilder builder,
            PathString url,
            string? method,
            IEnumerable<int> supportStatusCodes,
            IWorkflowExpression<string> content = default,
            IWorkflowExpression<string> contentType = default,
            IWorkflowExpression<string> authorization = default,
            IWorkflowExpression<HttpRequestHeaders> headers = default,
            bool readContent = false
        ) => builder.Then<SendHttpRequest>(x => x
            .WithUrl(url)
            .WithMethod(method ?? HttpMethods.Get)
            .WithContent(content)
            .WithSupportedStatusCodes(supportStatusCodes)
            .WithContentType(contentType)
            .WithAuthorization(authorization)
            .WithRequestHeaders(headers)
            .WithReadContent(readContent));

        public static IActivityBuilder SendHttpRequest(
            this IBuilder builder,
            PathString url,
            string? method,
            IEnumerable<int> supportStatusCodes,
            Func<ActivityExecutionContext, string> content = default,
            Func<ActivityExecutionContext, string> contentType = default,
            Func<ActivityExecutionContext, string> authorization = default,
            Func<ActivityExecutionContext, HttpRequestHeaders> headers = default,
            bool readContent = false
        ) => builder.Then<SendHttpRequest>(x => x
            .WithUrl(url)
            .WithMethod(method ?? HttpMethods.Get)
            .WithContent(content)
            .WithSupportedStatusCodes(supportStatusCodes)
            .WithContentType(contentType)
            .WithAuthorization(authorization)
            .WithRequestHeaders(headers)
            .WithReadContent(readContent));

        public static IActivityBuilder SendHttpRequest(
            this IBuilder builder,
            PathString url,
            string? method,
            IEnumerable<int> supportStatusCodes,
            Func<string> content = default,
            Func<string> contentType = default,
            Func<string> authorization = default,
            Func<HttpRequestHeaders> headers = default,
            bool readContent = false
        ) => builder.Then<SendHttpRequest>(x => x
            .WithUrl(url)
            .WithMethod(method ?? HttpMethods.Get)
            .WithContent(content)
            .WithSupportedStatusCodes(supportStatusCodes)
            .WithContentType(contentType)
            .WithAuthorization(authorization)
            .WithRequestHeaders(headers)
            .WithReadContent(readContent));

        public static IActivityBuilder SendHttpRequest(
            this IBuilder builder,
            PathString url,
            string? method,
            IEnumerable<int> supportStatusCodes,
            string content = default,
            string contentType = default,
            string authorization = default,
            HttpRequestHeaders headers = default,
            bool readContent = false
        ) => builder.Then<SendHttpRequest>(x => x
            .WithUrl(url)
            .WithMethod(method ?? HttpMethods.Get)
            .WithContent(content)
            .WithSupportedStatusCodes(supportStatusCodes)
            .WithContentType(contentType)
            .WithAuthorization(authorization)
            .WithRequestHeaders(headers)
            .WithReadContent(readContent));
    }
}