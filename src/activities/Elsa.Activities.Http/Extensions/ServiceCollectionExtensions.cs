using System;
using Elsa.Activities.Http;
using Elsa.Activities.Http.Indexes;
using Elsa.Activities.Http.Options;
using Elsa.Activities.Http.Parsers;
using Elsa.Activities.Http.RequestHandlers.Handlers;
using Elsa.Activities.Http.Services;
using Elsa.Activities.Http.Triggers;
using Elsa.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHttpActivities(this IServiceCollection services, Action<HttpActivityOptions>? configureOptions = null) =>
            services
                .AddHttpServices(configureOptions)
                .AddHttpActivitiesInternal();
        
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
                .AddIndexProvider<WorkflowInstanceByReceiveHttpRequestIndexProvider>()
                .AddDataMigration<Migrations>()
                .AddHttpContextAccessor()
                .AddNotificationHandlers(typeof(ServiceCollectionExtensions))
                .AddDataProtection();
            
            return services
                .AddRequestHandler<SignalRequestHandler>();
        }

        private static IServiceCollection AddHttpActivitiesInternal(this IServiceCollection services) =>
            services
                .AddActivity<ReceiveHttpRequest>()
                .AddActivity<WriteHttpResponse>()
                .AddActivity<SendHttpRequest>()
                .AddActivity<Redirect>();

        public static IServiceCollection AddRequestHandler<THandler>(this IServiceCollection services) where THandler : class, IRequestHandler => services.AddScoped<THandler>();
    }
}