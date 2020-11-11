using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Hosting;
using System;

namespace ElsaDashboard.Blazor.Server
{
    public class Program
    {
        public static bool UseBlazorServer = false;
        public static RenderMode RenderMode => UseBlazorServer ? RenderMode.Server: RenderMode.WebAssembly;
        private static Type StartupType => UseBlazorServer ? typeof(BlazorServerStartup) : typeof(BlazorWebAssemblyStartup);

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup(StartupType);
                });
    }
}
