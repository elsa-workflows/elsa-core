using System.Threading.Tasks;
using Elsa.Runtime.Hosting.Extensions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace SampleHost.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateWebHostBuilder(args).Build();
            await host.InitAsync();
            await host.RunAsync();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            ProgramExtensions.ConfigureTypeConverters();
            return WebHost.CreateDefaultBuilder<Startup>(args);
        }
    }
}