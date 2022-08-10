using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Multitenant;
using Elsa.Multitenancy;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.DocumentApproval
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddElsaServices()
                .AddControllers();
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // This will all go in the ROOT CONTAINER and is NOT TENANT SPECIFIC.

            var services = new ServiceCollection();

            builder.ConfigureElsaServices(services, elsa => elsa
                    .UseEntityFrameworkPersistence(ef => ef.UseSqlite())
                    .AddConsoleActivities()
                    .AddHttpActivities()
                    .AddWorkflow<DocumentApprovalWorkflow>());

            builder.Populate(services);
        }

        public void Configure(IApplicationBuilder app)
        {
            // Add HTTP activities middleware.
            app.UseHttpActivities();
            
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }

        public static MultitenantContainer ConfigureMultitenantContainer(IContainer container)
        {
            return MultitenantContainerFactory.CreateSampleMultitenantContainer(container);
        }
    }
}