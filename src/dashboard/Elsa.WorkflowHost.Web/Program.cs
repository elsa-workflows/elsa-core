using Elsa.TypeConverters;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Elsa.WorkflowHost.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ProgramExtensions.ConfigureTypeConverters();
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}