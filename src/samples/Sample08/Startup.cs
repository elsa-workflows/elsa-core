using Elsa.Activities.Email.Extensions;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.MassTransit.Extensions;
using Elsa.Activities.Timers.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using Sample08.Messages;
using Sample08.Workflows;

namespace Sample08
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
            services
                .AddElsa()
                .AddHttpActivities()
                .AddTimerActivities(options => options.Configure(x => x.SweepInterval = Duration.FromSeconds(10)))
                .AddEmailActivities(options => options.Bind(Configuration.GetSection("Smtp")))
                .AddRabbitMqActivities(
                    options => options.Bind(Configuration.GetSection("MassTransit:RabbitMq")),
                    typeof(CreateOrder),
                    typeof(OrderShipped))
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