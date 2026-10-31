using Elsa.Studio.Extensions;
using Elsa.Studio.Login.HttpMessageHandlers;
using Elsa.Studio.Models;
using Shared.Constants;

namespace Elsa.Studio.BlazorServer.Bootstrap
{
    public static class Backend
    {
        public static BackendApiConfig ConfigureBackend(this IServiceCollection services, IConfiguration configuration)
        {
            var backendApiConfig = new BackendApiConfig
            {
                ConfigureBackendOptions = options => configuration.GetSection(SettingKeys.Backend).Bind(options),
                ConfigureHttpClientBuilder = options => options.AuthenticationHandler = typeof(AuthenticatingApiHttpMessageHandler),
            };

            services.AddRemoteBackend(backendApiConfig);

            return backendApiConfig;
        }
    }
}
