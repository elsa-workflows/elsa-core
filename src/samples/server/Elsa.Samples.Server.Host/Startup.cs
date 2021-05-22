using Elsa.Activities.UserTask.Extensions;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;
using Elsa.Samples.Server.Host.Activities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

            services.AddControllers();

            services
                .AddActivityPropertyOptionsProvider<VehicleActivity>()
                .AddRuntimeSelectItemsProvider<VehicleActivity>()
                //.AddRedis(Configuration.GetConnectionString("Redis"))
                .AddElsa(elsa => elsa
                    //.WithContainerName(Configuration["ContainerName"] ?? System.Environment.MachineName)
                    .UseEntityFrameworkPersistence(ef => ef.UseSqlite())
                    //.UseRabbitMq(Configuration.GetConnectionString("RabbitMq"))
                    //.UseRebusCacheSignal()
                    //.UseRedisCacheSignal()
                    // .ConfigureDistributedLockProvider(options => options.UseProviderFactory(sp => name =>
                    // {
                    //     var connection = sp.GetRequiredService<IConnectionMultiplexer>();
                    //     return new RedisDistributedLock(name, connection.GetDatabase());
                    // }))
                    .AddConsoleActivities()
                    .AddHttpActivities(elsaSection.GetSection("Http").Bind)
                    .AddEmailActivities(elsaSection.GetSection("Smtp").Bind)
                    .AddQuartzTemporalActivities()
                    //.AddHangfireTemporalActivities(hangfire => hangfire.UseInMemoryStorage(), (_, hangfireServer) => hangfireServer.SchedulePollingInterval = TimeSpan.FromSeconds(5))
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