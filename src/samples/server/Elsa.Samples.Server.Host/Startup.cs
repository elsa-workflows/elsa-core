using Elsa.Activities.Telnyx.Extensions;
using Elsa.Activities.UserTask.Extensions;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;
using Elsa.Persistence.EntityFramework.SqlServer;
using Elsa.Samples.Server.Host.Activities;
using Elsa.Server.Hangfire.Extensions;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using NodaTime.Serialization.JsonNet;

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
            var hangfireSection = Configuration.GetSection("Hangfire");

            services.AddControllers();

            // Hangfire is required when using Hangfire Dispatchers.
            services
                .AddHangfire(configuration => configuration
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings(settings => settings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb))
                    .UseInMemoryStorage())
                .AddHangfireServer(options =>
                {
                    options.ConfigureForElsaDispatchers();
                    hangfireSection
                        .GetSection("Server")
                        .Bind(options);
                });

            services
                .AddActivityPropertyOptionsProvider<VehicleActivity>()
                .AddRuntimeSelectItemsProvider<VehicleActivity>()
                .AddElsa(elsa => elsa
                    .UseNonPooledEntityFrameworkPersistence(ef => ef.UseSqlite())
                    //.UseNonPooledEntityFrameworkPersistence(ef => ef.UseSqlServer("Server=LAPTOP-B76STK67;Database=Elsa;Integrated Security=true;MultipleActiveResultSets=True;"))
                    //.UseYesSqlPersistence()
                    
                    // Using Hangfire as the dispatcher for workflow execution in the background.
                    .UseHangfireDispatchers()
                    
                    .AddConsoleActivities()
                    .AddHttpActivities(elsaSection.GetSection("Http").Bind)
                    .AddEmailActivities(elsaSection.GetSection("Smtp").Bind)
                    .AddQuartzTemporalActivities()
                    .AddJavaScriptActivities()
                    .AddUserTaskActivities()
                    .AddTelnyx()
                    .AddActivitiesFrom<VehicleActivity>()
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
                .UseCors()
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    endpoints.MapTelnyxWebhook();
                });
        }
    }
}