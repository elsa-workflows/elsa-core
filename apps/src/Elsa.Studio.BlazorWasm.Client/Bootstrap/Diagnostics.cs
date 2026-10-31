using Elsa.Studio.BlazorServer.Helpers;
using Microsoft.ApplicationInsights.AspNetCore;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Constants;
using Shared.Options;

namespace Elsa.Studio.BlazorWasm.Client.Bootstrap
{
    public static class Diagnostics
    {
        public static IServiceCollection ConfigureDiagnostics(this IServiceCollection services, IConfiguration configuration)
        {
            var diagnosticOptions = configuration.GetOptionsOrDefault<DiagnosticOptions?>(SettingKeys.Diagnostics);
            if(diagnosticOptions is null)
                return services;

            switch (diagnosticOptions.Type)
            {
                case DiagnosticOptionType.ApplicationInsights:
                    services.ConfigureApplicationInsights(configuration);
                    break;
                default:
                    break;
            }

            return services;
        }

        public static IServiceCollection ConfigureApplicationInsights(this IServiceCollection services, IConfiguration configuration)
        {
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            var applicationInsightsOptions = configuration.GetOptions<ApplicationInsightsServiceOptions>(SettingKeys.ApplicationInsights);

            if (applicationInsightsOptions.ConnectionString != null || applicationInsightsOptions.InstrumentationKey != null)
            {
                applicationInsightsOptions.RequestCollectionOptions.TrackExceptions = true;
                applicationInsightsOptions.RequestCollectionOptions.InjectResponseHeaders = true;
                applicationInsightsOptions.EnableHeartbeat = true;
                services.AddSingleton(applicationInsightsOptions);
                services.AddApplicationInsightsTelemetry(applicationInsightsOptions);
            }

            return services;
        }
    }
}