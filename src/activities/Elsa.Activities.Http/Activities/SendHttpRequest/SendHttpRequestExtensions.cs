using System;
using System.Collections.Generic;
using Elsa.Activities.Http;
using Elsa.Activities.Http.Models;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class SendHttpRequestExtensions
    {
        public static SendHttpRequest WithUrl(this SendHttpRequest activity, IWorkflowExpression<PathString> value) => activity.With(x => x.Url, value);
        public static SendHttpRequest WithUrl(this SendHttpRequest activity, Func<ActivityExecutionContext, PathString> value) => activity.With(x => x.Url, new CodeExpression<PathString>(value));
        public static SendHttpRequest WithUrl(this SendHttpRequest activity, Func<PathString> value) => activity.With(x => x.Url, new CodeExpression<PathString>(value));
        public static SendHttpRequest WithUrl(this SendHttpRequest activity, PathString value) => activity.With(x => x.Url, new CodeExpression<PathString>(value));
        public static SendHttpRequest WithMethod(this SendHttpRequest activity, string value) => activity.With(x => x.Method, value);
        public static SendHttpRequest WithContent(this SendHttpRequest activity, IWorkflowExpression<string>? value) => activity.With(x => x.Content, value);
        public static SendHttpRequest WithContent(this SendHttpRequest activity, Func<ActivityExecutionContext, string> value) => activity.With(x => x.Content, new CodeExpression<string>(value));
        public static SendHttpRequest WithContent(this SendHttpRequest activity, Func<string> value) => activity.With(x => x.Content, new CodeExpression<string>(value));
        public static SendHttpRequest WithContent(this SendHttpRequest activity, string value) => activity.With(x => x.Content, new CodeExpression<string>(value));
        public static SendHttpRequest WithContentType(this SendHttpRequest activity, IWorkflowExpression<string> value) => activity.With(x => x.ContentType, value);
        public static SendHttpRequest WithContentType(this SendHttpRequest activity, Func<ActivityExecutionContext, string> value) => activity.With(x => x.ContentType, new CodeExpression<string>(value));
        public static SendHttpRequest WithContentType(this SendHttpRequest activity, Func<string> value) => activity.With(x => x.ContentType, new CodeExpression<string>(value));
        public static SendHttpRequest WithContentType(this SendHttpRequest activity, string value) => activity.With(x => x.ContentType, new CodeExpression<string>(value));
        public static SendHttpRequest WithAuthorization(this SendHttpRequest activity, IWorkflowExpression<string>? value) => activity.With(x => x.Authorization, value);
        public static SendHttpRequest WithAuthorization(this SendHttpRequest activity, Func<ActivityExecutionContext, string> value) => activity.With(x => x.Authorization, new CodeExpression<string>(value));
        public static SendHttpRequest WithAuthorization(this SendHttpRequest activity, Func<string> value) => activity.With(x => x.Authorization, new CodeExpression<string>(value));
        public static SendHttpRequest WithAuthorization(this SendHttpRequest activity, string value) => activity.With(x => x.Authorization, new CodeExpression<string>(value));
        public static SendHttpRequest WithRequestHeaders(this SendHttpRequest activity, IWorkflowExpression<HttpRequestHeaders> value) => activity.With(x => x.RequestHeaders, value);
        public static SendHttpRequest WithRequestHeaders(this SendHttpRequest activity, Func<ActivityExecutionContext, HttpRequestHeaders> value) => activity.With(x => x.RequestHeaders, new CodeExpression<HttpRequestHeaders>(value));
        public static SendHttpRequest WithRequestHeaders(this SendHttpRequest activity, Func<HttpRequestHeaders> value) => activity.With(x => x.RequestHeaders, new CodeExpression<HttpRequestHeaders>(value));
        public static SendHttpRequest WithRequestHeaders(this SendHttpRequest activity, HttpRequestHeaders value) => activity.With(x => x.RequestHeaders, new CodeExpression<HttpRequestHeaders>(value));
        public static SendHttpRequest WithReadContent(this SendHttpRequest activity, bool value) => activity.With(x => x.ReadContent, value);
        public static SendHttpRequest WithSupportedStatusCodes(this SendHttpRequest activity, IEnumerable<int> value) => activity.With(x => x.SupportedStatusCodes, value);
    }
}