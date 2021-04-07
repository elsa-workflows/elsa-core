using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using ElsaDashboard.Application;
using ElsaDashboard.WebAssembly.Extensions;

namespace ElsaDashboard.Samples.WebAssembly
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            var services = builder.Services;

            builder.RootComponents.Add<App>("#app");
            
            // Change the Elsa Server URL to point to wherever you have hosted Elsa. In this example, Elsa is either hosted by "ElsaDashboard.Samples.Monolith" or "Elsa.Samples.Server.Host"
            services.AddElsaDashboardUI(options => options.ElsaServerUrl = new Uri("https://localhost:11000"));
            
            // When the web assembly project is hosted directly from e.g. blob storage, the URL to the gRPC backend needs to be specified explicitly.
            // When hosted from ElsaDashboard.Samples.Monolith or ElsaDashboard.Samples.Server, we don't need to specify a backend URL.
            //services.AddElsaDashboardBackend(options => options.BackendUrl = new Uri("https://localhost:11000"));
            
            services.AddElsaDashboardBackend();

            await builder.Build().RunAsync();
        }
    }
}