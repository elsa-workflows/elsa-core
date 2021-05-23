using System;
using Elsa.Activities.UserTask.Extensions;
using Elsa.Caching.Rebus.Extensions;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.SqlServer;
using Elsa.Rebus.RabbitMq;
using Elsa.Samples.Server.Host.Activities;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace Elsa.Samples.Server.Host
{
    public class Startup
    {
        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Environment = environment;
            Configuration = configuration;
        }

        private IWebHostEnvironment Environment { get; }
        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var elsaSection = Configuration.GetSection("Elsa");
            var sqlServerConnectionString = Configuration.GetConnectionString("SqlServer");

            services.AddControllers();

            services
                .AddActivityPropertyOptionsProvider<VehicleActivity>()
                .AddRuntimeSelectItemsProvider<VehicleActivity>()
                //.AddRedis(Configuration.GetConnectionString("Redis"))
                .AddElsa(elsa => elsa
                    .WithContainerName(Configuration["ContainerName"] ?? System.Environment.MachineName)
                    .UseEntityFrameworkPersistence(ef => ef.UseSqlServer(sqlServerConnectionString))
                    .UseRabbitMq(Configuration.GetConnectionString("RabbitMq"))
                    .UseRebusCacheSignal()
                    //.UseRedisCacheSignal()
                    .AddConsoleActivities()
                    .AddHttpActivities(elsaSection.GetSection("Http").Bind)
                    .AddEmailActivities(elsaSection.GetSection("Smtp").Bind)
                    .AddQuartzTemporalActivities()
                    // .AddQuartzTemporalActivities(configureQuartz: quartz => quartz.UsePersistentStore(store =>
                    // {
                    //     store.UseJsonSerializer();
                    //     store.UseSqlServer(sqlServerConnectionString);
                    //     store.UseClustering();
                    // }))
                    //.AddHangfireTemporalActivities(hangfire => hangfire.UseInMemoryStorage(), (_, hangfireServer) => hangfireServer.SchedulePollingInterval = TimeSpan.FromSeconds(5))
                    //.AddHangfireTemporalActivities(hangfire => hangfire.UseSqlServerStorage(sqlServerConnectionString), (_, hangfireServer) => hangfireServer.SchedulePollingInterval = TimeSpan.FromSeconds(5))
                    .AddJavaScriptActivities()
                    .AddUserTaskActivities()
                    .AddActivitiesFrom<Startup>()
                    .AddWorkflowsFrom<Startup>()
                );

            // Elsa API endpoints.
            services
                .AddElsaApiEndpoints()
                .AddElsaSwagger();

            // Allow arbitrary client browser apps to access the API for demo purposes only.
            // In a production environment, make sure to allow only origins you trust.
            services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().WithExposedHeaders("Content-Disposition")));
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Elsa"));
            }

            app
                .UseCors()
                .UseHttpActivities()
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });
        }
    }
}