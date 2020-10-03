using System;
using System.Net;
using Elsa.Activities.Http.Models;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class WriteHttpResponseExtensions
    {
        public static WriteHttpResponse WithStatusCode(this WriteHttpResponse activity, HttpStatusCode value) => activity.With(x => x.StatusCode, value);
        public static WriteHttpResponse WithContent(this WriteHttpResponse activity, Func<ActivityExecutionContext, string>? value) => activity.With(x => x.Content, value);
        public static WriteHttpResponse WithContent(this WriteHttpResponse activity, Func<string>? value) => activity.With(x => x.Content, value);
        public static WriteHttpResponse WithContent(this WriteHttpResponse activity, string? value) => activity.With(x => x.Content, value);
        public static WriteHttpResponse WithContentType(this WriteHttpResponse activity, string? value) => activity.With(x => x.ContentType, value);
        public static WriteHttpResponse WithResponseHeaders(this WriteHttpResponse activity, Func<ActivityExecutionContext, HttpResponseHeaders>? value) => activity.With(x => x.ResponseHeaders, value);
        public static WriteHttpResponse WithResponseHeaders(this WriteHttpResponse activity, Func<HttpResponseHeaders>? value) => activity.With(x => x.ResponseHeaders, value);
        public static WriteHttpResponse WithResponseHeaders(this WriteHttpResponse activity, HttpResponseHeaders? value) => activity.With(x => x.ResponseHeaders, value);
    }
}