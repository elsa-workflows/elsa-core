using System;
using Elsa.Activities.Http;
using Elsa.Activities.Http.Indexes;
using Elsa.Activities.Http.Options;
using Elsa.Activities.Http.Parsers;
using Elsa.Activities.Http.RequestHandlers.Handlers;
using Elsa.Activities.Http.Services;
using Elsa.Activities.Http.Triggers;
using Elsa.Data;
using Elsa.Extensions;
using Elsa.Triggers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHttpActivities(this IServiceCollection services, Action<OptionsBuilder<HttpActivityOptions>> options = null) =>
            services
                .AddHttpServices(options)
                .AddHttpActivitiesInternal();
        
        public static IServiceCollection AddHttpServices(this IServiceCollection services, Action<OptionsBuilder<HttpActivityOptions>> options = null)
        {
            options?.Invoke(services.AddOptions<HttpActivityOptions>());

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
                .AddSingleton<ITriggerProvider, ReceiveHttpRequestTriggerProvider>()
                .AddIndexProvider<WorkflowInstanceByReceiveHttpRequestIndexProvider>()
                .AddDataMigration<Migrations>()
                .AddHttpContextAccessor()
                .AddNotificationHandlers(typeof(ServiceCollectionExtensions))
                .AddDataProtection();
            
            return services
                .AddRequestHandler<TriggerRequestHandler>()
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