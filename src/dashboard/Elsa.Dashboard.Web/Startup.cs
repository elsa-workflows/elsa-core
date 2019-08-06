using System.Data;
using Elsa.Dashboard.Extensions;
using Elsa.Persistence.YesSql.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using YesSql.Provider.Sqlite;
using YesSql;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Elsa.Dashboard.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Latest);

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
            app
                .UseCors(x => x.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod())
                .UseMvcWithDefaultRoute()
                .UseElsaDashboard();
        }
    }
}