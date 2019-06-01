using Elsa.Persistence.FileSystem.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SampleDashboard.Web
{
    public class Startup
    {
        private IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddOrchardCms(orchard =>
                {
                    orchard.ConfigureServices(tenant =>
                    {
                        tenant
                            .AddFileSystemWorkflowDefinitionStoreProvider(Configuration.GetSection("Workflows:FileSystem"))
                            .AddFileSystemWorkflowInstanceStoreProvider(Configuration.GetSection("Workflows:FileSystem"));
                    });
                });

//          TODO: Temporarily disabled until I figured out why it's broken. 
//          services
//              .AddRouting(options => options.LowercaseUrls = true)
//              .AddOrchardCoreTheming();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseOrchardCore();
        }
    }
}