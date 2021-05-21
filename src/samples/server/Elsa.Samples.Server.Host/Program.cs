using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Elsa.Samples.Server.Host
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => webBuilder
                    .UseStaticWebAssets()
                    .UseStartup<Startup>())
                // .UseOrleans(siloBuilder => siloBuilder
                //     .UseLocalhostClustering()
                //     .Configure<ClusterOptions>(options =>
                //     {
                //         options.ClusterId = "localhost";
                //         options.ServiceId = "elsa-workflows";
                //     })
                //     .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IWorkflowDefinitionGrain).Assembly).WithReferences())
                //     .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback))
            ;
    }
}