using Elsa.Studio.BlazorServer.Helpers;
using HealthChecks.UI.Client;
using Shared.Constants;
using HealthCheckOptions = Shared.Options.HealthCheckOptions;

namespace Elsa.Studio.BlazorWasm.Client.Bootstrap
{
    public static class HealthChecks
    {
        public static IServiceCollection ConfigureHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            var healthCheckOptions = configuration.GetOptions<HealthCheckOptions>(SettingKeys.HealthChecks);

            if (!healthCheckOptions.Disabled)
                services.AddHealthChecks();

            return services;
        }

        public static void ConfigureHealthCheckEndpoints(this WebApplication app)
        {
            var healthCheckOptions = app.Configuration.GetOptions<HealthCheckOptions>(SettingKeys.HealthChecks);

            if (healthCheckOptions.Disabled)
                return;

            foreach(var endpoint in healthCheckOptions.Endpoints)
            {
                var route = endpoint.Endpoint.StartsWith('/') 
                    ? endpoint.Endpoint 
                    : "/" + endpoint.Endpoint;

                app.MapHealthChecks(route, new()
                {
                    Predicate = check => check.Tags.Contains(endpoint.Tag),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
            }            
        }
    }
}
