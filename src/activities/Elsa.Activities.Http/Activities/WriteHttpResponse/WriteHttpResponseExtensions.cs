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
        /// <summary>
        /// Specify the status code that is returned with the HTTP response.
        /// </summary>
        /// <param name="value">The status code of the HTTP response</param>
        /// <returns>A <see cref="ISetupActivity"/> that can be used to further change properties of <see cref="WriteHttpResponse"/></returns>
        public static ISetupActivity<WriteHttpResponse> WithStatusCode(this ISetupActivity<WriteHttpResponse> activity, Func<ActivityExecutionContext, ValueTask<HttpStatusCode>> value) => activity.Set(x => x.StatusCode, value);
        
        /// <inheritdoc cref="WithStatusCode(ISetupActivity{WriteHttpResponse}, Func{ActivityExecutionContext, ValueTask{HttpStatusCode}})"/>
        public static ISetupActivity<WriteHttpResponse> WithStatusCode(this ISetupActivity<WriteHttpResponse> activity, Func<ValueTask<HttpStatusCode>> value) => activity.Set(x => x.StatusCode, value);
        
        /// <inheritdoc cref="WithStatusCode(ISetupActivity{WriteHttpResponse}, Func{ActivityExecutionContext, ValueTask{HttpStatusCode}})"/>
        public static ISetupActivity<WriteHttpResponse> WithStatusCode(this ISetupActivity<WriteHttpResponse> activity, Func<ActivityExecutionContext, HttpStatusCode> value) => activity.Set(x => x.StatusCode, value);
        
        /// <inheritdoc cref="WithStatusCode(ISetupActivity{WriteHttpResponse}, Func{ActivityExecutionContext, ValueTask{HttpStatusCode}})"/>
        public static ISetupActivity<WriteHttpResponse> WithStatusCode(this ISetupActivity<WriteHttpResponse> activity, Func<HttpStatusCode> value) => activity.Set(x => x.StatusCode, value);
        
        /// <inheritdoc cref="WithStatusCode(ISetupActivity{WriteHttpResponse}, Func{ActivityExecutionContext, ValueTask{HttpStatusCode}})"/>
        public static ISetupActivity<WriteHttpResponse> WithStatusCode(this ISetupActivity<WriteHttpResponse> activity, HttpStatusCode value) => activity.Set(x => x.StatusCode, value);


        /// <summary>
        /// Specify the HTTP Reponse's body.
        /// </summary>
        /// <param name="value">The body of the HTTP Response.</param>
        /// <returns>A <see cref="ISetupActivity"/> that can be used to further change properties of <see cref="WriteHttpResponse"/></returns>
        public static ISetupActivity<WriteHttpResponse> WithContent(this ISetupActivity<WriteHttpResponse> activity, Func<ActivityExecutionContext, ValueTask<object?>> value) => activity.Set(x => x.Content, value);
        
        /// <inheritdoc cref="WithContent(ISetupActivity{WriteHttpResponse}, Func{ActivityExecutionContext, ValueTask{object?}})"/>
        public static ISetupActivity<WriteHttpResponse> WithContent(this ISetupActivity<WriteHttpResponse> activity, Func<ValueTask<object?>> value) => activity.Set(x => x.Content, value);
        
        /// <inheritdoc cref="WithContent(ISetupActivity{WriteHttpResponse}, Func{ActivityExecutionContext, ValueTask{object?}})"/>
        public static ISetupActivity<WriteHttpResponse> WithContent(this ISetupActivity<WriteHttpResponse> activity, Func<ActivityExecutionContext, object?> value) => activity.Set(x => x.Content, value);
        
        /// <inheritdoc cref="WithContent(ISetupActivity{WriteHttpResponse}, Func{ActivityExecutionContext, ValueTask{object?}})"/>
        public static ISetupActivity<WriteHttpResponse> WithContent(this ISetupActivity<WriteHttpResponse> activity, Func<object?> value) => activity.Set(x => x.Content, value);
        
        /// <inheritdoc cref="WithContent(ISetupActivity{WriteHttpResponse}, Func{ActivityExecutionContext, ValueTask{object?}})"/>
        public static ISetupActivity<WriteHttpResponse> WithContent(this ISetupActivity<WriteHttpResponse> activity, object? value) => activity.Set(x => x.Content, value);

        
        /// <summary>
        /// Specify the Content-Type header of the HTTP Response
        /// </summary>
        /// <param name="value">The value of the Content-Type header</param>
        /// <returns>A <see cref="ISetupActivity"/> that can be used to further change properties of <see cref="WriteHttpResponse"/></returns>
        public static ISetupActivity<WriteHttpResponse> WithContentType(this ISetupActivity<WriteHttpResponse> activity, Func<ActivityExecutionContext, ValueTask<object?>> value) => activity.Set(x => x.ContentType, value);
        
        /// <inheritdoc cref="WithContent(ISetupActivity{WriteHttpResponse}, Func{ActivityExecutionContext, ValueTask{object?}})"/>
        public static ISetupActivity<WriteHttpResponse> WithContentType(this ISetupActivity<WriteHttpResponse> activity, Func<ValueTask<object?>> value) => activity.Set(x => x.ContentType, value);
        
        /// <inheritdoc cref="WithContent(ISetupActivity{WriteHttpResponse}, Func{ActivityExecutionContext, ValueTask{object?}})"/>
        public static ISetupActivity<WriteHttpResponse> WithContentType(this ISetupActivity<WriteHttpResponse> activity, Func<ActivityExecutionContext, object?> value) => activity.Set(x => x.ContentType, value);
        
        /// <inheritdoc cref="WithContent(ISetupActivity{WriteHttpResponse}, Func{ActivityExecutionContext, ValueTask{object?}})"/>
        public static ISetupActivity<WriteHttpResponse> WithContentType(this ISetupActivity<WriteHttpResponse> activity, Func<object?> value) => activity.Set(x => x.ContentType, value);
        
        /// <inheritdoc cref="WithContent(ISetupActivity{WriteHttpResponse}, Func{ActivityExecutionContext, ValueTask{object?}})"/>
        public static ISetupActivity<WriteHttpResponse> WithContentType(this ISetupActivity<WriteHttpResponse> activity, object? value) => activity.Set(x => x.ContentType, value);

        
        /// <summary>
        /// Additional response headers to be added to the HTTP Response.
        /// </summary>
        /// <param name="value">A <see cref="Dictionary"/> of response headers.</param>
        /// <returns>A <see cref="ISetupActivity"/> that can be used to further change properties of <see cref="WriteHttpResponse"/></returns>
        public static ISetupActivity<WriteHttpResponse> WithResponseHeaders(this ISetupActivity<WriteHttpResponse> activity, Func<ActivityExecutionContext, ValueTask<HttpResponseHeaders?>> value) => activity.Set(x => x.ResponseHeaders, value);
        
        /// <inheritdoc cref="WithResponseHeaders(ISetupActivity{WriteHttpResponse}, Func{ActivityExecutionContext, ValueTask{HttpResponseHeaders?}})"/>
        public static ISetupActivity<WriteHttpResponse> WithResponseHeaders(this ISetupActivity<WriteHttpResponse> activity, Func<ValueTask<HttpResponseHeaders?>> value) => activity.Set(x => x.ResponseHeaders, value);
        
        /// <inheritdoc cref="WithResponseHeaders(ISetupActivity{WriteHttpResponse}, Func{ActivityExecutionContext, ValueTask{HttpResponseHeaders?}})"/>
        public static ISetupActivity<WriteHttpResponse> WithResponseHeaders(this ISetupActivity<WriteHttpResponse> activity, Func<ActivityExecutionContext, HttpResponseHeaders?> value) => activity.Set(x => x.ResponseHeaders, value);
        
        /// <inheritdoc cref="WithResponseHeaders(ISetupActivity{WriteHttpResponse}, Func{ActivityExecutionContext, ValueTask{HttpResponseHeaders?}})"/>
        public static ISetupActivity<WriteHttpResponse> WithResponseHeaders(this ISetupActivity<WriteHttpResponse> activity, Func<HttpResponseHeaders?> value) => activity.Set(x => x.ResponseHeaders, value);
        
        /// <inheritdoc cref="WithResponseHeaders(ISetupActivity{WriteHttpResponse}, Func{ActivityExecutionContext, ValueTask{HttpResponseHeaders?}})"/>
        public static ISetupActivity<WriteHttpResponse> WithResponseHeaders(this ISetupActivity<WriteHttpResponse> activity, HttpResponseHeaders? value) => activity.Set(x => x.ResponseHeaders, value);
    }
}