using System.Data;
using System.IO;
using Elsa.Dashboard.Conventions;
using Elsa.Dashboard.Extensions;
using Elsa.Persistence.YesSql.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using YesSql.Provider.Sqlite;
using YesSql;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Elsa.Dashboard.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }
        public IHostingEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc(options => options.Conventions.Add(new AddLocalhostFilterConvention()))
                .SetCompatibilityVersion(CompatibilityVersion.Latest)
                .AddRazorOptions(options =>
                    {
                        // Workaround to get Razor views in Elsa.Dashboard recompiled when changing .cshtml.
                        if(Environment.IsDevelopment())
                            options.FileProviders.Add(new PhysicalFileProvider(Path.Combine(Environment.ContentRootPath, @"..\Elsa.Dashboard")));
                    }
                );

            services
                .AddYesSql(
                    options => options
                        .UseSqLite(@"Data Source=c:\data\elsa.yessql.db;Cache=Shared", IsolationLevel.ReadUncommitted)
                        .UseDefaultIdGenerator()
                        .SetTablePrefix("elsa_")
                )
                .AddYesSqlWorkflowDefinitionStore()
                .AddYesSqlWorkflowInstanceStore()
                .AddElsaDashboard();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();
        }
    }
}