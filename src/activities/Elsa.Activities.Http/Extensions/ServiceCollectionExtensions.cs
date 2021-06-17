using System;
using Elsa;
using Elsa.Activities.Http;
using Elsa.Activities.Http.Bookmarks;
using Elsa.Activities.Http.JavaScript;
using Elsa.Activities.Http.Liquid;
using Elsa.Activities.Http.Options;
using Elsa.Activities.Http.Parsers;
using Elsa.Activities.Http.Parsers.Request;
using Elsa.Activities.Http.Parsers.Response;
using Elsa.Activities.Http.Services;
using Elsa.Scripting.Liquid.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddHttpActivities(this ElsaOptionsBuilder options, Action<HttpActivityOptions>? configureOptions = null)
        {
            options.Services.AddHttpServices(configureOptions);
            options.AddHttpActivitiesInternal();
            return options;
        }

        public static IServiceCollection AddHttpServices(this IServiceCollection services, Action<HttpActivityOptions>? configureOptions = null)
        {
            if (configureOptions != null) 
                services.Configure(configureOptions);

            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddHttpClient(nameof(SendHttpRequest));

            services
                .AddSingleton<IHttpRequestBodyParser, DefaultHttpRequestBodyParser>()
                .AddSingleton<IHttpRequestBodyParser, JsonHttpRequestBodyParser>()
                .AddSingleton<IHttpRequestBodyParser, FormHttpRequestBodyParser>()
                .AddSingleton<IHttpResponseContentReader, DefaultHttpResponseContentReader>()
                .AddSingleton<IHttpResponseContentReader, JsonHttpResponseContentReader>()
                .AddSingleton<IHttpResponseContentReader, FileResponseContentReader>()
                .AddSingleton<IActionContextAccessor, ActionContextAccessor>()
                .AddSingleton<IAbsoluteUrlProvider, DefaultAbsoluteUrlProvider>()
                .AddBookmarkProvider<HttpEndpointBookmarkProvider>()
                .AddHttpContextAccessor()
                .AddNotificationHandlers(typeof(ConfigureJavaScriptEngine))
                .AddLiquidFilter<SignalUrlFilter>("signal_url")
                .AddJavaScriptTypeDefinitionProvider<HttpTypeDefinitionProvider>()
                .AddDataProtection();

            return services;
        }

        private static ElsaOptionsBuilder AddHttpActivitiesInternal(this ElsaOptionsBuilder options) =>
            options
                .AddActivity<HttpEndpoint>()
                .AddActivity<WriteHttpResponse>()
                .AddActivity<SendHttpRequest>()
                .AddActivity<Redirect>();
    }
}