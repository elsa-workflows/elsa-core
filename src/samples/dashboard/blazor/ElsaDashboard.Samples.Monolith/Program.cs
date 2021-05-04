using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Hosting;

namespace ElsaDashboard.Samples.Monolith
{
    public class Program
    {
        public static BlazorRuntimeModel RuntimeModel => BlazorRuntimeModel.Server;
        public static RenderMode RenderMode => RuntimeModel == BlazorRuntimeModel.Server ? RenderMode.ServerPrerendered : RenderMode.WebAssemblyPrerendered;

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}