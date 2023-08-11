using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class HttpEndpointExtensions
    {
        /// <summary>
        /// Specify the path of the HTTP Endpoint.
        /// </summary>
        /// <param name="path">The path of the <see cref="HttpEndpoint"/> Activity</param>
        /// <returns>A <see cref="ISetupActivity"/> that can be used to further change properties of <see cref="HttpEndpoint"/></returns>
        public static ISetupActivity<HttpEndpoint> WithPath(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, ValueTask<string>> path) => activity.Set(x => x.Path, path);

        /// <inheritdoc cref="WithPath(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{string}})"/>
        public static ISetupActivity<HttpEndpoint> WithPath(this ISetupActivity<HttpEndpoint> activity, Func<ValueTask<string>> path) => activity.Set(x => x.Path, path);
        
        /// <inheritdoc cref="WithPath(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{string}})"/>
        public static ISetupActivity<HttpEndpoint> WithPath(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, string> path) => activity.Set(x => x.Path, path);
        
        /// <inheritdoc cref="WithPath(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{string}})"/>
        public static ISetupActivity<HttpEndpoint> WithPath(this ISetupActivity<HttpEndpoint> activity, Func<string> path) => activity.Set(x => x.Path, path);
        
        /// <inheritdoc cref="WithPath(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{string}})"/>
        public static ISetupActivity<HttpEndpoint> WithPath(this ISetupActivity<HttpEndpoint> activity, string path) => activity.Set(x => x.Path, path);


        /// <summary>
        /// Specify the <see href="https://datatracker.ietf.org/doc/html/rfc7231#section-4.3">HTTP Verbs</see> that can be used to access this endpoint.
        /// </summary>
        /// <param name="value">A list of HTTP methods (GET, POST, PUT, PATCH, DELETE, etc.)</param>
        /// <returns>A <see cref="ISetupActivity"/> that can be used to further change properties of <see cref="HttpEndpoint"/></returns>
        public static ISetupActivity<HttpEndpoint> WithMethods(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, ValueTask<IEnumerable<string>>> value) =>
            activity.Set(x => x.Methods, async context => new HashSet<string>(await value(context)));
        
        /// <inheritdoc cref="WithMethods(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{IEnumerable{string}}})"/>
        public static ISetupActivity<HttpEndpoint> WithMethods(this ISetupActivity<HttpEndpoint> activity, Func<ValueTask<IEnumerable<string>>> value) => activity.Set(x => x.Methods, async () => await value());
        
        /// <inheritdoc cref="WithMethods(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{IEnumerable{string}}})"/>
        public static ISetupActivity<HttpEndpoint> WithMethods(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, IEnumerable<string>> value) =>
            activity.Set(x => x.Methods, context => new HashSet<string>(value(context)));
        
        /// <inheritdoc cref="WithMethods(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{IEnumerable{string}}})"/>
        public static ISetupActivity<HttpEndpoint> WithMethods(this ISetupActivity<HttpEndpoint> activity, Func<IEnumerable<string>> value) => activity.Set(x => x.Methods, () => new HashSet<string>(value()));
        
        /// <inheritdoc cref="WithMethods(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{IEnumerable{string}}})"/>
        public static ISetupActivity<HttpEndpoint> WithMethods(this ISetupActivity<HttpEndpoint> activity, IEnumerable<string> value) => activity.Set(x => x.Methods, new HashSet<string>(value));

        
        /// <summary>
        /// Specify the <see href="https://datatracker.ietf.org/doc/html/rfc7231#section-4.3">HTTP Verb</see> that can be used to access this endpoint.
        /// </summary>
        /// <param name="value">A single HTTP method (GET, POST, PUT, PATCH, DELETE, etc.)</param>
        /// <returns>A <see cref="ISetupActivity"/> that can be used to further change properties of <see cref="HttpEndpoint"/></returns>
        public static ISetupActivity<HttpEndpoint> WithMethod(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, ValueTask<string>> value) => activity.WithMethods(async context => new[] { await value(context) });
        
        /// <inheritdoc cref="WithMethod(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{string}})"/>
        public static ISetupActivity<HttpEndpoint> WithMethod(this ISetupActivity<HttpEndpoint> activity, Func<ValueTask<string>> value) => activity.WithMethods(async () => new[] { await value() });
        
        /// <inheritdoc cref="WithMethod(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{string}})"/>
        public static ISetupActivity<HttpEndpoint> WithMethod(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, string> value) => activity.WithMethods(context => new[] { value(context) });
        
        /// <inheritdoc cref="WithMethod(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{string}})"/>
        public static ISetupActivity<HttpEndpoint> WithMethod(this ISetupActivity<HttpEndpoint> activity, Func<string> value) => activity.WithMethods(() => new[] { value() });
        
        /// <inheritdoc cref="WithMethod(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{string}})"/>
        public static ISetupActivity<HttpEndpoint> WithMethod(this ISetupActivity<HttpEndpoint> activity, string value) => activity.WithMethods(new[] { value });

        
        /// <summary>
        /// Sets the value of <see cref="HttpActivity.ReadContent"/> which dictates whether the request content body should be read and stored as part of the HTTP request model.
        /// </summary>
        /// <param name="value">Whether the request content body should be read and stored as part of the HTTP request model.</param>
        /// <returns>A <see cref="ISetupActivity"/> that can be used to further change properties of <see cref="HttpEndpoint"/></returns>
        public static ISetupActivity<HttpEndpoint> WithReadContent(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, ValueTask<bool>> value) => activity.Set(x => x.ReadContent, value);
        
        /// <inheritdoc cref="WithReadContent(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{bool}})"/>
        public static ISetupActivity<HttpEndpoint> WithReadContent(this ISetupActivity<HttpEndpoint> activity, Func<ValueTask<bool>> value) => activity.Set(x => x.ReadContent, value);
        
        /// <inheritdoc cref="WithReadContent(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{bool}})"/>
        public static ISetupActivity<HttpEndpoint> WithReadContent(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, bool> value) => activity.Set(x => x.ReadContent, value);
        
        /// <inheritdoc cref="WithReadContent(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{bool}})"/>
        public static ISetupActivity<HttpEndpoint> WithReadContent(this ISetupActivity<HttpEndpoint> activity, Func<bool> value) => activity.Set(x => x.ReadContent, value);
        
        /// <inheritdoc cref="WithReadContent(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{bool}})"/>
        public static ISetupActivity<HttpEndpoint> WithReadContent(this ISetupActivity<HttpEndpoint> activity, bool value = true) => activity.Set(x => x.ReadContent, value);

        
        /// <summary>
        /// The <see cref="Type"/> to parse the received request content into if <seealso cref="HttpActivity.ReadContent"/> is set to true.
        /// </summary>
        /// <param name="value">The type to parse the request into.</param>
        /// <returns>A <see cref="ISetupActivity"/> that can be used to further change properties of <see cref="HttpEndpoint"/></returns>
        public static ISetupActivity<HttpEndpoint> WithTargetType(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, ValueTask<Type?>> value) => activity.Set(x => x.TargetType, value).WithReadContent();
        
        /// <inheritdoc cref="WithTargetType(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{Type?}})"/>
        public static ISetupActivity<HttpEndpoint> WithTargetType(this ISetupActivity<HttpEndpoint> activity, Func<ValueTask<Type?>> value) => activity.Set(x => x.TargetType, value).WithReadContent();
        
        /// <inheritdoc cref="WithTargetType(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{Type?}})"/>
        public static ISetupActivity<HttpEndpoint> WithTargetType(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, Type?> value) => activity.Set(x => x.TargetType, value).WithReadContent();
        
        /// <inheritdoc cref="WithTargetType(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{Type?}})"/>
        public static ISetupActivity<HttpEndpoint> WithTargetType(this ISetupActivity<HttpEndpoint> activity, Func<Type?> value) => activity.Set(x => x.TargetType, value).WithReadContent();
        
        /// <inheritdoc cref="WithTargetType(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{Type?}})"/>
        public static ISetupActivity<HttpEndpoint> WithTargetType(this ISetupActivity<HttpEndpoint> activity, Type? value) => activity.Set(x => x.TargetType, value).WithReadContent();
        
        /// <inheritdoc cref="WithTargetType(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{Type?}})"/>
        /// <typeparam name="T">The type to parse the request into.</typeparam>
        public static ISetupActivity<HttpEndpoint> WithTargetType<T>(this ISetupActivity<HttpEndpoint> activity) => activity.Set(x => x.TargetType, typeof(T)).WithReadContent();

        
        /// <summary>
        /// Sets whether or not this endpoint requires authorization to use.
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="value">Whether or not authorization is used.</param>
        /// <returns>A <see cref="ISetupActivity"/> that can be used to further change properties of <see cref="HttpEndpoint"/></returns>
        public static ISetupActivity<HttpEndpoint> WithAuthorize(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, ValueTask<bool>> value) => activity.Set(x => x.Authorize, value);
        
        /// <inheritdoc cref="WithAuthorize(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{bool}})"/>
        public static ISetupActivity<HttpEndpoint> WithAuthorize(this ISetupActivity<HttpEndpoint> activity, Func<ValueTask<bool>> value) => activity.Set(x => x.Authorize, value);
        
        /// <inheritdoc cref="WithAuthorize(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{bool}})"/>
        public static ISetupActivity<HttpEndpoint> WithAuthorize(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, bool> value) => activity.Set(x => x.Authorize, value);
        
        /// <inheritdoc cref="WithAuthorize(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{bool}})"/>
        public static ISetupActivity<HttpEndpoint> WithAuthorize(this ISetupActivity<HttpEndpoint> activity, Func<bool> value) => activity.Set(x => x.Authorize, value);
        
        /// <inheritdoc cref="WithAuthorize(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{bool}})"/>
        public static ISetupActivity<HttpEndpoint> WithAuthorize(this ISetupActivity<HttpEndpoint> activity, bool value = true) => activity.Set(x => x.Authorize, value);

        
        /// <summary>
        /// Sets the policy name that is evaluated when a user calls the endpoint. If it fails then the request is forbidden
        /// </summary>
        /// <param name="value">The policy name that the user is challenged with.</param>
        /// <returns>A <see cref="ISetupActivity"/> that can be used to further change properties of <see cref="HttpEndpoint"/></returns>
        public static ISetupActivity<HttpEndpoint> WithPolicy(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, ValueTask<string?>> value) => activity.Set(x => x.Policy, value);
        
        /// <inheritdoc cref="WithPolicy(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{string?}})"/>
        public static ISetupActivity<HttpEndpoint> WithPolicy(this ISetupActivity<HttpEndpoint> activity, Func<ValueTask<string?>> value) => activity.Set(x => x.Policy, value);
        
        /// <inheritdoc cref="WithPolicy(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{string?}})"/>
        public static ISetupActivity<HttpEndpoint> WithPolicy(this ISetupActivity<HttpEndpoint> activity, Func<ActivityExecutionContext, string?> value) => activity.Set(x => x.Policy, value);
        
        /// <inheritdoc cref="WithPolicy(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{string?}})"/>
        public static ISetupActivity<HttpEndpoint> WithPolicy(this ISetupActivity<HttpEndpoint> activity, Func<string?> value) => activity.Set(x => x.Policy, value);
        
        /// <inheritdoc cref="WithPolicy(ISetupActivity{HttpEndpoint}, Func{ActivityExecutionContext, ValueTask{string?}})"/>
        public static ISetupActivity<HttpEndpoint> WithPolicy(this ISetupActivity<HttpEndpoint> activity, string? value) => activity.Set(x => x.Policy, value);
        
        
        /// <summary>
        /// Sets whether or not this endpoint requires authorization with a custom header to use
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="value">Whether or not authorization with custom header is used.</param>
        /// <param name="customHeaderName">The name of the custom header.</param>
        /// <param name="customHeaderValue">The value of the custom header</param>
        /// <returns>A <see cref="ISetupActivity"/> that can be used to further change properties of <see cref="HttpEndpoint"/></returns>
        public static ISetupActivity<HttpEndpoint> WithAuthorizeWithCustomHeader(this ISetupActivity<HttpEndpoint> activity, bool value, string customHeaderName, string customHeaderValue)
        {
            return activity.Set(x => x.AuthorizeWithCustomHeader, value)
                .Set(x => x.CustomHeaderName, customHeaderName)
                .Set(x => x.CustomHeaderValue, customHeaderValue);
        }
    }
}