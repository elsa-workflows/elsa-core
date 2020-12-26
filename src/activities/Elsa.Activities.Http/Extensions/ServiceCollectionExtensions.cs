using System;
using Elsa;
using Elsa.Activities.Http;
using Elsa.Activities.Http.Handlers;
using Elsa.Activities.Http.Options;
using Elsa.Activities.Http.Parsers;
using Elsa.Activities.Http.RequestHandlers.Handlers;
using Elsa.Activities.Http.Services;
using Elsa.Activities.Http.Triggers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptions AddHttpActivities(this ElsaOptions options, Action<HttpActivityOptions>? configureOptions = null)
        {
            options.Services.AddHttpServices(configureOptions);
            options.AddHttpActivitiesInternal();
            return options;
        }

        public static IServiceCollection AddHttpServices(this IServiceCollection services, Action<HttpActivityOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddHttpClient(nameof(SendHttpRequest));

            services
                .AddSingleton<ITokenService, TokenService>()
                .AddSingleton<IHttpRequestBodyParser, DefaultHttpRequestBodyParser>()
                .AddSingleton<IHttpRequestBodyParser, JsonHttpRequestBodyParser>()
                .AddSingleton<IHttpRequestBodyParser, FormHttpRequestBodyParser>()
                .AddSingleton<IHttpResponseBodyParser, DefaultHttpResponseBodyParser>()
                .AddSingleton<IHttpResponseBodyParser, JsonHttpResponseBodyParser>()
                .AddSingleton<IActionContextAccessor, ActionContextAccessor>()
                .AddSingleton<IAbsoluteUrlProvider, DefaultAbsoluteUrlProvider>()
                .AddTriggerProvider<ReceiveHttpRequestTriggerProvider>()
                .AddHttpContextAccessor()
                .AddNotificationHandlers(typeof(HttpJavaScriptHandler))
                .AddDataProtection();
            
            return services
                .AddRequestHandler<SignalRequestHandler>();
        }

        private static ElsaOptions AddHttpActivitiesInternal(this ElsaOptions options) =>
            options
                .AddActivity<HttpRequestReceived>()
                .AddActivity<WriteHttpResponse>()
                .AddActivity<SendHttpRequest>()
                .AddActivity<Redirect>();

        public static IServiceCollection AddRequestHandler<THandler>(this IServiceCollection services) where THandler : class, IRequestHandler => services.AddScoped<THandler>();
    }
}