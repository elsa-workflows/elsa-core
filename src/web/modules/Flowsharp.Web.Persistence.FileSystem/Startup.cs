using System;
using Flowsharp.Persistence;
using Flowsharp.Persistence.FileSystem;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;

namespace Flowsharp.Web.Persistence.FileSystem
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IWorkflowDefinitionStore, FileSystemWorkflowDefinitionStore>();
        }

        public override void Configure(IApplicationBuilder builder, IRouteBuilder routes, IServiceProvider serviceProvider)
        {
            
        }
    }
}
