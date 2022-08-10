using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace ElsaDashboard.Samples.AspNetCore.Monolith
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(new AutofacMultitenantServiceProviderFactory(Startup.ConfigureMultitenantContainer))
                .ConfigureWebHostDefaults(webBuilder => { webBuilder
                    .UseStaticWebAssets()
                    .UseStartup<Startup>(); });
    }
}   