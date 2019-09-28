using System.IO;
using Elsa.Dashboard.Extensions;
using Elsa.Persistence.EntityFrameworkCore.Extensions;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Elsa.Dashboard.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        private IConfiguration Configuration { get; }
        private IHostingEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc( /*options => options.Conventions.Add(new AddLocalhostFilterConvention())*/)
                .SetCompatibilityVersion(CompatibilityVersion.Latest)

                // This is entirely optional and only useful when developing the Elsa.Dashboard project.
                .AddRazorOptions(
                    options =>
                    {
                        // Workaround to get Razor views in Elsa.Dashboard recompiled when changing .cshtml.
                        if (Environment.IsDevelopment())
                            options.FileProviders.Add(
                                new PhysicalFileProvider(
                                    Path.Combine(Environment.ContentRootPath, @"..\Elsa.Dashboard")
                                )
                            );
                    }
                );

            services
                .AddEntityFrameworkCore(
                    options => options
                        .UseSqlite(Configuration.GetConnectionString("Sqlite"))
                )
                .AddEntityFrameworkCoreWorkflowDefinitionStore()
                .AddEntityFrameworkCoreWorkflowInstanceStore()
                .AddElsaDashboard(options => options.DiscoveredActivities());
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app
                .UseDeveloperExceptionPage()
                .UseStaticFiles()
                .UseMvcWithDefaultRoute()
                .UseWelcomePage();
        }
    }
}