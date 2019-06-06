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
            await CreateWebHostBuilder(args).Build().RunAsync();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            ProgramExtensions.ConfigureTypeConverters();
            return WebHost.CreateDefaultBuilder<Startup>(args);
        }
    }
}