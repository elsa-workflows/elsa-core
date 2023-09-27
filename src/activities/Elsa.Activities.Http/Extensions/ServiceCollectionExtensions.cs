using System;
using Elsa;
using Elsa.Activities.Http;
using Elsa.Activities.Http.Bookmarks;
using Elsa.Activities.Http.Consumers;
using Elsa.Activities.Http.Contracts;
using Elsa.Activities.Http.JavaScript;
using Elsa.Activities.Http.Options;
using Elsa.Activities.Http.Parsers.Request;
using Elsa.Activities.Http.Parsers.Response;
using Elsa.Activities.Http.Scripting.JavaScript;
using Elsa.Activities.Http.Scripting.Liquid;
using Elsa.Activities.Http.Services;
using Elsa.Activities.Http.StartupTasks;
using Elsa.Events;
using Elsa.Options;
using Elsa.Runtime;
using Elsa.Scripting.JavaScript.Providers;
using Elsa.Scripting.Liquid.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddHttpActivities(this ElsaOptionsBuilder options, Action<HttpActivityOptions>? configureOptions = default, Action<IHttpClientBuilder>? configureHttpClient = default) =>
            options
                .AddHttpServices(configureOptions, configureHttpClient)
                .AddHttpActivitiesInternal();

        public static ElsaOptionsBuilder AddHttpServices(this ElsaOptionsBuilder options, Action<HttpActivityOptions>? configureOptions = default, Action<IHttpClientBuilder>? configureHttpClient = default)
        {
            var services = options.Services;

            if (configureOptions != null)
                services.Configure(configureOptions);

            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            var httpClientBuilder = services.AddHttpClient(nameof(SendHttpRequest));
            configureHttpClient?.Invoke(httpClientBuilder);

            services.AddAuthorizationCore();

            services
                .AddSingleton<IHttpRequestBodyParser, DefaultHttpRequestBodyParser>()
                .AddSingleton<IHttpRequestBodyParser, JsonHttpRequestBodyParser>()
                .AddSingleton<IHttpRequestBodyParser, XmlHttpRequestBodyParser>()
                .AddSingleton<IHttpRequestBodyParser, FormHttpRequestBodyParser>()
                .AddSingleton<IHttpResponseContentReader, PlainTextHttpResponseContentReader>()
                .AddSingleton<IHttpResponseContentReader, TypedHttpResponseContentReader>()
                .AddSingleton<IHttpResponseContentReader, JTokenHttpResponseContentReader>()
                .AddSingleton<IHttpResponseContentReader, FileHttpResponseContentReader>()
                .AddSingleton<IActionContextAccessor, ActionContextAccessor>()
                .AddSingleton<IActivityTypeDefinitionRenderer, HttpEndpointTypeDefinitionRenderer>()
                .AddSingleton<IAbsoluteUrlProvider, DefaultAbsoluteUrlProvider>()
                .AddSingleton<IRouteMatcher, RouteMatcher>()
                .AddSingleton<IRouteTable, RouteTable>()
                .AddSingleton<AllowAnonymousHttpEndpointAuthorizationHandler>()
                .AddSingleton(sp => sp.GetRequiredService<IOptions<HttpActivityOptions>>().Value.HttpEndpointAuthorizationHandlerFactory(sp))
                .AddSingleton(sp => sp.GetRequiredService<IOptions<HttpActivityOptions>>().Value.HttpEndpointWorkflowFaultHandlerFactory(sp))
                .AddBookmarkProvider<HttpEndpointBookmarkProvider>()
                .AddHttpContextAccessor()
                .AddNotificationHandlers(typeof(ConfigureJavaScriptEngine))
                .AddLiquidFilter<SignalUrlFilter>("signal_url")
                .AddJavaScriptTypeDefinitionProvider<HttpTypeDefinitionProvider>()
                .AddStartupTask<UpdateRouteTableWithBookmarks>()

                .AddMemoryCache()
                .AddDataProtection();

            options.AddPubSubConsumer<UpdateRouteTable, TriggerIndexingFinished>("WorkflowManagementEvents");
            options.AddPubSubConsumer<UpdateRouteTable, TriggersDeleted>("WorkflowManagementEvents");
            options.AddPubSubConsumer<UpdateRouteTable, BookmarkIndexingFinished>("WorkflowManagementEvents");
            options.AddPubSubConsumer<UpdateRouteTable, BookmarksDeleted>("WorkflowManagementEvents");

            return options;
        }

        private static ElsaOptionsBuilder AddHttpActivitiesInternal(this ElsaOptionsBuilder options) =>
            options
                .AddActivity<HttpEndpoint>()
                .AddActivity<WriteHttpResponse>()
                .AddActivity<SendHttpRequest>()
                .AddActivity<Redirect>();
    }
}