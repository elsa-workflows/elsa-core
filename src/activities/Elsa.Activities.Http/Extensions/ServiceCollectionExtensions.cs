using Elsa.Activities.Http.Activities;
using Elsa.Activities.Http.Formatters;
using Elsa.Activities.Http.RequestHandlers.Handlers;
using Elsa.Activities.Http.Scripting;
using Elsa.Activities.Http.Services;
using Elsa.Core.Extensions;
using Elsa.Scripting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Activities.Http.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHttpActivities(this IServiceCollection services)
        {
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddHttpClient(nameof(HttpRequestAction));
            
            services
                .AddActivity<HttpRequestTrigger>()
                .AddActivity<HttpResponseAction>()
                .AddActivity<HttpRequestAction>()
                .AddActivity<SignalEvent>();

            services
                .AddSingleton<ISharedAccessSignatureService, SharedAccessSignatureService>()
                .AddSingleton<IContentFormatter, NullContentFormatter>()
                .AddSingleton<IContentFormatter, JsonContentFormatter>()
                .AddSingleton<IScriptEngineConfigurator, HttpScriptEngineConfigurator>()
                .AddSingleton<IActionContextAccessor, ActionContextAccessor>()
                .AddSingleton<IAbsoluteUrlProvider, DefaultAbsoluteUrlProvider>()
                .AddHttpContextAccessor()
                .AddDataProtection();

            return services
                .AddScoped(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext)
                .AddRequestHandler<TriggerRequestHandler>()
                .AddRequestHandler<SignalRequestHandler>();
        }

        public static IServiceCollection AddRequestHandler<THandler>(this IServiceCollection services) where THandler : class, IRequestHandler
        {
            return services.AddScoped<THandler>();
        }
    }
}