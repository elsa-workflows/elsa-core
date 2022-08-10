using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Multitenant;
using Elsa.Multitenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.SendHttp
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddElsaServices();
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // This will all go in the ROOT CONTAINER and is NOT TENANT SPECIFIC.

            var services = new ServiceCollection();

            builder.ConfigureElsaServices(services, elsa => elsa
                    .AddHttpActivities()
                    .AddWorkflowsFrom<Startup>());

            builder.Populate(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseHttpActivities();
        }

        public static MultitenantContainer ConfigureMultitenantContainer(IContainer container)
        {
            return MultitenantContainerFactory.CreateSampleMultitenantContainer(container);
        }
    }
}
