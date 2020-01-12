using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Http;
using Elsa.Activities.Http.Models;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class WriteHttpResponseBuilderExtensions
    {
        public static ActivityBuilder WriteHttpResponse(
            this IBuilder builder,
            HttpStatusCode statusCode,
            IWorkflowExpression<string> content = default,
            string contentType = default,
            IWorkflowExpression<HttpResponseHeaders> headers = default
        ) => builder.Then<WriteHttpResponse>(x => x
            .WithStatusCode(statusCode)
            .WithContent(content)
            .WithContentType(contentType)
            .WithResponseHeaders(headers));
        
        public static ActivityBuilder WriteHttpResponse(
            this IBuilder builder,
            HttpStatusCode statusCode,
            Func<ActivityExecutionContext, string> content = default,
            string contentType = default,
            Func<ActivityExecutionContext, HttpResponseHeaders> headers = default
        ) => builder.Then<WriteHttpResponse>(x => x
            .WithStatusCode(statusCode)
            .WithContent(content)
            .WithContentType(contentType)
            .WithResponseHeaders(headers));
        
        public static ActivityBuilder WriteHttpResponse(
            this IBuilder builder,
            HttpStatusCode statusCode,
            Func<string> content = default,
            string contentType = default,
            Func<HttpResponseHeaders> headers = default
        ) => builder.Then<WriteHttpResponse>(x => x
            .WithStatusCode(statusCode)
            .WithContent(content)
            .WithContentType(contentType)
            .WithResponseHeaders(headers));
        
        public static ActivityBuilder WriteHttpResponse(
            this IBuilder builder,
            HttpStatusCode statusCode,
            string content = default,
            string contentType = default,
            HttpResponseHeaders headers = default
        ) => builder.Then<WriteHttpResponse>(x => x
            .WithStatusCode(statusCode)
            .WithContent(content)
            .WithContentType(contentType)
            .WithResponseHeaders(headers));
    }
}