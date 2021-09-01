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
            services
                .AddElsa(options => options
                    .AddConsoleActivities()
                    .StartWorkflow<InfiniteLoopingWorkflow>());
            
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

        public void Configure(IApplicationBuilder app)
        {
        }
    }
}