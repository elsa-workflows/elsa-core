using System;
using System.IO;
using Elsa.Persistence;
using Elsa.Web.Persistence.FileSystem.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrchardCore.Environment.Shell;
using OrchardCore.FileStorage.FileSystem;
using OrchardCore.Modules;

namespace Elsa.Web.Persistence.FileSystem
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IWorkflowsFileStore>(serviceProvider =>
            {
                var shellOptions = serviceProvider.GetRequiredService<IOptions<ShellOptions>>().Value;
                var shellSettings = serviceProvider.GetRequiredService<ShellSettings>();
                var mediaPath = Path.Combine(shellOptions.ShellsApplicationDataPath, shellOptions.ShellsContainerName, shellSettings.Name, "elsa");
                var fileStore = new FileSystemStore(mediaPath);

                return new WorkflowsFileStore(fileStore);
            });

            services.AddScoped<IWorkflowStore, FileSystemWorkflowStore>();
        }

        public override void Configure(IApplicationBuilder builder, IRouteBuilder routes, IServiceProvider serviceProvider)
        {
            
        }
    }
}
