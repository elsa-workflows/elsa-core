using Elsa.Dashboard.Extensions;
using Elsa.Persistence.EntityFrameworkCore.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dashboard.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            services.AddElsaDashboard(
                options => options.DiscoverActivities(),
                elsa => elsa.WithEntityFrameworkCoreProvider(
                    provider => provider.UseSqlite(Configuration.GetConnectionString("Sqlite")))
                    .WithWorkflowDefinitionStore()
                    .WithWorkflowInstanceStore());
        }

        public void Configure(IApplicationBuilder app)
        {
            app
                .UseDeveloperExceptionPage()
                .UseStaticFiles()
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapControllers())
                .UseWelcomePage();
        }
    }
}