using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Multitenant;
using Elsa.Multitenancy;
using Elsa.Options;
using Elsa.Services.Stability;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace Elsa.Samples.InfiniteLoopDetection
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddElsaServices();

            // Configure infinite loop detection.
            services.Configure<LoopDetectorOptions>(options =>
            {
                options.DefaultLoopDetector = typeof(ActivityExecutionCountLoopDetector);
                options.DefaultLoopHandler = typeof(CooldownLoopHandler);
            });
            
            // Induce a cooldown after 100 iterations of the same activity within the current burst of execution.
            services.Configure<ActivityExecutionCountLoopDetectorOptions>(options => options.MaxExecutionCount = 100);

            // Allow a maximum of 3 cooldown events.
            services.Configure<CooldownLoopHandlerOptions>(options =>
            {
                options.MaxCooldownEvents = 3;
                options.CooldownPeriod = Duration.FromSeconds(7);
            });
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // This will all go in the ROOT CONTAINER and is NOT TENANT SPECIFIC.

            var services = new ServiceCollection();

            builder
                .ConfigureElsaServices(services, elsa => elsa
                .AddConsoleActivities()
                .StartWorkflow<InfiniteLoopingWorkflow>());

            builder.Populate(services);
        }

        public void Configure(IApplicationBuilder app)
        {
        }

        public static MultitenantContainer ConfigureMultitenantContainer(IContainer container)
        {
            return MultitenantContainerFactory.CreateSampleMultitenantContainer(container);
        }
    }
}