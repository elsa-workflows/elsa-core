using Elsa.Studio.Extensions;
using Elsa.Studio.Login.HttpMessageHandlers;
using Elsa.Studio.Models;
using Shared.Constants;
using System.Runtime.InteropServices.JavaScript;

namespace Elsa.Studio.BlazorWasm.Client.Bootstrap
{
    public static class Backend
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public static BackendApiConfig ConfigureBackend(this IServiceCollection services, IConfiguration configuration)
        {
            var backendUrl = JSHost.GlobalThis.GetPropertyAsJSObject("elsaConfig")?.GetPropertyAsString("backendUrl") ?? configuration.GetValue<string>("Backend:Url")!;

            var backendApiConfig = new BackendApiConfig
            {
                ConfigureBackendOptions = options => options.Url = new(backendUrl),
                ConfigureHttpClientBuilder = options =>
                {
                    options.AuthenticationHandler = typeof(AuthenticatingApiHttpMessageHandler);
                }
            };

            services.AddRemoteBackend(backendApiConfig);

            return backendApiConfig;
        }
    }
}
