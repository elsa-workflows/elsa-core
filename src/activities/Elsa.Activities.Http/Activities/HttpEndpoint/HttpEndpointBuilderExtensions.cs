using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class HttpEndpointBuilderExtensions
    {
        /// <summary>
        /// Creates an HTTP Endpoint activity
        /// </summary>
        /// <param name="setup">An <see cref="ISetupActivity"/> used to set the properties of the HTTP Endpoint Activity.</param>
        /// <example>
        /// <code>
        /// builder
        ///     .HttpEndpoint(setup => setup
        ///         .WithPath("/path")
        ///         .WithMethod(HttpMethods.Get));
        /// </code>
        /// </example>
        /// <returns>The <see cref="IActivityBuilder"/> with the Http Endpoint activity built onto it.</returns>
        /// <inheritdoc cref="BuilderExtensions.Then{T}(IBuilder, Action{ISetupActivity{T}}?, Action{IActivityBuilder}?, int, string?)"/>
        public static IActivityBuilder HttpEndpoint(this IBuilder builder, Action<ISetupActivity<HttpEndpoint>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        /// <param name="path">The path of the HTTP Endpoint. Allows caller to introduce the <see cref="ActivityExecutionContext"/> when deciding the endpoint's path.</param>
        /// <example>
        /// <code>
        /// builder
        ///     .HttpEndpoint(async context => context
        ///         string? path = await context.GetNamedActivityPropertyAsync("PathGeneratorActivity", "SubPath", "");
        ///         return "/path/" + path;
        ///     });
        /// </code>
        /// </example>
        /// <inheritdoc cref="HttpEndpoint(IBuilder, Action{ISetupActivity{HttpEndpoint}}, int, string?)"/>
        public static IActivityBuilder HttpEndpoint(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<string>> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.HttpEndpoint(setup => setup.WithPath(path).WithMethod(HttpMethods.Get), lineNumber, sourceFile);

        /// <example>
        /// <code>
        /// <![CDATA[
        /// builder
        ///     .HttpEndpoint(context => 
        ///         context.GetInput<string>());
        /// ]]>
        /// </code>
        /// </example>
        /// <inheritdoc cref="HttpEndpoint(IBuilder, Func{ActivityExecutionContext, ValueTask{string}}, int, string?)"/>
        public static IActivityBuilder HttpEndpoint(this IBuilder builder, Func<ActivityExecutionContext, string> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.HttpEndpoint(setup => setup.WithPath(path).WithMethod(HttpMethods.Get), lineNumber, sourceFile);

        /// <param name="path">The path of the HTTP Endpoint.</param>
        /// <example>
        /// <code>
        /// builder
        ///     .HttpEndpoint(async () => "/path");
        /// </code>
        /// </example>
        /// <inheritdoc cref="HttpEndpoint(IBuilder, Func{ActivityExecutionContext, ValueTask{string}}, int, string?)"/>
        public static IActivityBuilder HttpEndpoint(this IBuilder builder, Func<ValueTask<string>> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.HttpEndpoint(setup => setup.WithPath(path).WithMethod(HttpMethods.Get), lineNumber, sourceFile);


        /// <example>
        /// <code>
        /// builder
        ///     .HttpEndpoint(() => "/path");
        /// </code>
        /// </example>
        /// <inheritdoc cref="HttpEndpoint(IBuilder, Func{ValueTask{string}}, int, string?)"/>
        public static IActivityBuilder HttpEndpoint(this IBuilder builder, Func<string> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.HttpEndpoint(setup => setup.WithPath(path).WithMethod(HttpMethods.Get), lineNumber, sourceFile);

        /// <example>
        /// <code>
        /// builder
        ///     .HttpEndpoint("/path");
        /// </code>
        /// </example>
        /// <inheritdoc cref="HttpEndpoint(IBuilder, Func{ValueTask{string}}, int, string?)"/>
        public static IActivityBuilder HttpEndpoint(this IBuilder builder, string path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.HttpEndpoint(setup => setup.WithPath(path).WithMethod(HttpMethods.Get), lineNumber, sourceFile);

        /// <example>
        /// <code>
        /// <![CDATA[
        /// builder
        ///     .HttpEndpoint<RequestObject>(context => 
        ///         context.GetInput<string>());
        /// ]]>
        /// </code>
        /// </example>
        /// <inheritdoc cref="HttpEndpoint(IBuilder, Func{ActivityExecutionContext, ValueTask{string}}, int, string?)"/>
        public static IActivityBuilder HttpEndpoint<T>(this IBuilder builder, Func<ActivityExecutionContext, string> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.HttpEndpoint(activity => activity.WithPath(path).WithMethod(HttpMethods.Post).WithTargetType<T>(), lineNumber, sourceFile);

        /// <summary>
        /// Creates an HTTP Endpoint activity that parses the received request into type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type the received request should be parsed to.</typeparam>
        /// <param name="path">The path of the HTTP Endpoint.</param>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// builder
        ///     .HttpEndpoint<RequestObject>(() => "/path");
        /// ]]>
        /// </code>
        /// </example>
        /// <inheritdoc cref="HttpEndpoint(IBuilder, Func{ValueTask{string}}, int, string?)"/>
        public static IActivityBuilder HttpEndpoint<T>(this IBuilder builder, Func<string> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.HttpEndpoint(activity => activity.WithPath(path).WithMethod(HttpMethods.Post).WithTargetType<T>(), lineNumber, sourceFile);

        /// <example>
        /// <code>
        /// <![CDATA[
        /// builder
        ///     .HttpEndpoint<RequestObject>("/path");
        /// ]]>
        /// </code>
        /// </example>
        /// <inheritdoc cref="HttpEndpoint{T}(IBuilder, Func{string}, int, string?)"/>
        public static IActivityBuilder HttpEndpoint<T>(this IBuilder builder, string path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.HttpEndpoint(activity => activity.WithPath(path).WithMethod(HttpMethods.Post).WithTargetType<T>(), lineNumber, sourceFile);
    }
}