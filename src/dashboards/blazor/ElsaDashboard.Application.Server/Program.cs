using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Hosting;

namespace ElsaDashboard.Application.Server
{
    public class Program
    {
        public static bool UseBlazorServer = true;
        public static bool UseBlazorWebAssembly => !UseBlazorServer;
        public static RenderMode RenderMode => UseBlazorServer ? RenderMode.ServerPrerendered: RenderMode.WebAssemblyPrerendered;

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
