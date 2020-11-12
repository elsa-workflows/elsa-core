using System;
using System.Collections.Specialized;
using Elsa.Activities.Email.Extensions;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.MassTransit.Extensions;
using Elsa.Activities.Timers.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using Sample27.Controllers;
using Sample27.Services;
using Sample27.Workflows;

namespace Sample27
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
                .AddControllers();

            var scheduler = CreateScheduler();
            var massTransitBuilder = new AzureServiceBusSchedulerMassTransitBuilder(scheduler)
            {
                Options = o => o.Bind(Configuration.GetSection("MassTransit:AzureServiceBus"))
            };

            services
                .AddElsa()
                .AddHttpActivities()
                .AddTimerActivities(options => options.Configure(x => x.SweepInterval = Duration.FromSeconds(10)))
                .AddEmailActivities(options => options.Bind(Configuration.GetSection("Smtp")))
                .AddMassTransitSchedulingActivities(massTransitBuilder, options => options.Bind(Configuration.GetSection("MassTransit:AzureServiceBus:MessageSchedule")))
                .AddWorkflow<CartTrackingWorkflow>()
                .AddScoped<ICarts, Carts>()
                .AddSingleton(scheduler)
                .AddSingleton<IHostedService, QuartzHostedService>(); // Add a hosted service to stat and stop the quartz scheduler

            IScheduler CreateScheduler()
            {
                LogProvider.SetCurrentLogProvider(new QuartzConsoleLogProvider());

                ISchedulerFactory schedulerFactory = new StdSchedulerFactory(new NameValueCollection()
                {
                    {"quartz.scheduler.instanceName", "Sample-QuartzScheduler"},
                    {"quartz.scheduler.instanceId", "AUTO"},
                    {"quartz.threadPool.type", "Quartz.Simpl.SimpleThreadPool, Quartz"},
                    {"quartz.threadPool.threadCount", "4"},
                    {"quartz.jobStore.misfireThreshold", "60000"},
                    {"quartz.jobStore.type", "Quartz.Simpl.RAMJobStore, Quartz"},
                });

                var scheduler = schedulerFactory.GetScheduler().GetAwaiter().GetResult();

                return scheduler;
            }
        }

        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app
                .UseStaticFiles()
                .UseHttpActivities()
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapControllers())
                .UseWelcomePage();
        }

        private class QuartzConsoleLogProvider : ILogProvider
        {
            public Logger GetLogger(string name)
            {
                return (level, func, exception, parameters) =>
                {
                    if (level >= LogLevel.Debug && func != null)
                    {
                        Console.WriteLine("[" + DateTime.Now.ToLongTimeString() + "] [" + level + "] " + func(), parameters);
                    }
                    return true;
                };
            }

            public IDisposable OpenNestedContext(string message)
            {
                throw new NotImplementedException();
            }

            public IDisposable OpenMappedContext(string key, string value)
            {
                throw new NotImplementedException();
            }
        }
    }
}