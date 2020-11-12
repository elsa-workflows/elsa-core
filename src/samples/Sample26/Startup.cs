using Elsa.Activities.Email.Extensions;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.MassTransit;
using Elsa.Activities.MassTransit.Extensions;
using Elsa.Activities.MassTransit.Options;
using Elsa.Activities.Timers.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using Sample26.Messages;
using Sample26.Workflows;

namespace Sample26
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
            var massTransitBuilder = new DefaultAzureServiceBusMassTransitBuilder
            {
                MessageTypes = new[]
                {
                   typeof(CreateOrder),
                   typeof(OrderShipped)
                },
                Options = o => o.Bind(Configuration.GetSection("MassTransit:AzureServiceBus"))
            };

            services
                .AddElsa()
                .AddHttpActivities()
                .AddTimerActivities(options => options.Configure(x => x.SweepInterval = Duration.FromSeconds(10)))
                .AddEmailActivities(options => options.Bind(Configuration.GetSection("Smtp")))
                .AddMassTransitActivities(massTransitBuilder)
                .AddWorkflow<CreateOrderWorkflow>()
                .AddWorkflow<HandleOrderWorkflow>();
        }

        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseHttpActivities();
        }
    }
}