using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using ElsaDashboard.WebAssembly.Extensions;

namespace ElsaDashboard.Samples.WebAssembly
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            var services = builder.Services;

            //builder.RootComponents.Add<App>("#app");
            services.AddScoped(sp => new HttpClient {BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)});
            services.AddElsaDashboardUI();
            services.AddElsaDashboardBackend();

            await builder.Build().RunAsync();
        }
    }
}