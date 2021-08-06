using System;
using System.Net;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class WriteHttpResponseExtensions
    {
        public static ISetupActivity<WriteHttpResponse> WithStatusCode(this ISetupActivity<WriteHttpResponse> activity, Func<ActivityExecutionContext, ValueTask<HttpStatusCode>> value) => activity.Set(x => x.StatusCode, value);
        public static ISetupActivity<WriteHttpResponse> WithStatusCode(this ISetupActivity<WriteHttpResponse> activity, Func<ValueTask<HttpStatusCode>> value) => activity.Set(x => x.StatusCode, value);
        public static ISetupActivity<WriteHttpResponse> WithStatusCode(this ISetupActivity<WriteHttpResponse> activity, Func<ActivityExecutionContext, HttpStatusCode> value) => activity.Set(x => x.StatusCode, value);
        public static ISetupActivity<WriteHttpResponse> WithStatusCode(this ISetupActivity<WriteHttpResponse> activity, Func<HttpStatusCode> value) => activity.Set(x => x.StatusCode, value);
        public static ISetupActivity<WriteHttpResponse> WithStatusCode(this ISetupActivity<WriteHttpResponse> activity, HttpStatusCode value) => activity.Set(x => x.StatusCode, value);
        
        public static ISetupActivity<WriteHttpResponse> WithContent(this ISetupActivity<WriteHttpResponse> activity, Func<ActivityExecutionContext, ValueTask<object?>> value) => activity.Set(x => x.Content, value);
        public static ISetupActivity<WriteHttpResponse> WithContent(this ISetupActivity<WriteHttpResponse> activity, Func<ValueTask<object?>> value) => activity.Set(x => x.Content, value);
        public static ISetupActivity<WriteHttpResponse> WithContent(this ISetupActivity<WriteHttpResponse> activity, Func<ActivityExecutionContext, object?> value) => activity.Set(x => x.Content, value);
        public static ISetupActivity<WriteHttpResponse> WithContent(this ISetupActivity<WriteHttpResponse> activity, Func<object?> value) => activity.Set(x => x.Content, value);
        public static ISetupActivity<WriteHttpResponse> WithContent(this ISetupActivity<WriteHttpResponse> activity, object? value) => activity.Set(x => x.Content, value);

        public static ISetupActivity<WriteHttpResponse> WithContentType(this ISetupActivity<WriteHttpResponse> activity, Func<ActivityExecutionContext, ValueTask<object?>> value) => activity.Set(x => x.ContentType, value);
        public static ISetupActivity<WriteHttpResponse> WithContentType(this ISetupActivity<WriteHttpResponse> activity, Func<ValueTask<object?>> value) => activity.Set(x => x.ContentType, value);
        public static ISetupActivity<WriteHttpResponse> WithContentType(this ISetupActivity<WriteHttpResponse> activity, Func<ActivityExecutionContext, object?> value) => activity.Set(x => x.ContentType, value);
        public static ISetupActivity<WriteHttpResponse> WithContentType(this ISetupActivity<WriteHttpResponse> activity, Func<object?> value) => activity.Set(x => x.ContentType, value);
        public static ISetupActivity<WriteHttpResponse> WithContentType(this ISetupActivity<WriteHttpResponse> activity, object? value) => activity.Set(x => x.ContentType, value);
        
        public static ISetupActivity<WriteHttpResponse> WithResponseHeaders(this ISetupActivity<WriteHttpResponse> activity, Func<ActivityExecutionContext, ValueTask<HttpResponseHeaders?>> value) => activity.Set(x => x.ResponseHeaders, value);
        public static ISetupActivity<WriteHttpResponse> WithResponseHeaders(this ISetupActivity<WriteHttpResponse> activity, Func<ValueTask<HttpResponseHeaders?>> value) => activity.Set(x => x.ResponseHeaders, value);
        public static ISetupActivity<WriteHttpResponse> WithResponseHeaders(this ISetupActivity<WriteHttpResponse> activity, Func<ActivityExecutionContext, HttpResponseHeaders?> value) => activity.Set(x => x.ResponseHeaders, value);
        public static ISetupActivity<WriteHttpResponse> WithResponseHeaders(this ISetupActivity<WriteHttpResponse> activity, Func<HttpResponseHeaders?> value) => activity.Set(x => x.ResponseHeaders, value);
        public static ISetupActivity<WriteHttpResponse> WithResponseHeaders(this ISetupActivity<WriteHttpResponse> activity, HttpResponseHeaders? value) => activity.Set(x => x.ResponseHeaders, value);
    }
}