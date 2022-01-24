using Elsa.Providers.Workflows;
using Elsa.Retention.Extensions;
using Elsa.WorkflowTesting.Api.Extensions;
using Hangfire;
using Hangfire.SQLite;
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

            services.AddControllers();

            // TODO: Determine startup types based on project references, similar to Orchard Core's Targets.props for Applications and Modules.
            // Note that simply loading all referenced assemblies will not include assemblies where no types have been referenced in this project (due to assembly trimming?).
            var startups = new[]
            {
                typeof(Elsa.Activities.Console.Startup),
                typeof(Elsa.Activities.Http.Startup),
                typeof(Elsa.Activities.AzureServiceBus.Startup),
                typeof(Elsa.Activities.Conductor.Startup),
                typeof(Elsa.Activities.UserTask.Startup),
                typeof(Elsa.Activities.Temporal.Quartz.Startup),
                typeof(Elsa.Activities.Temporal.Hangfire.Startup),
                typeof(Elsa.Activities.Email.Startup),
                typeof(Elsa.Activities.Telnyx.Startup),
                typeof(Elsa.Activities.File.Startup),
                typeof(Elsa.Activities.RabbitMq.Startup),
                typeof(Elsa.Activities.Mqtt.Startup),
                typeof(Persistence.EntityFramework.Sqlite.Startup),
                typeof(Persistence.EntityFramework.SqlServer.Startup),
                typeof(Persistence.EntityFramework.MySql.Startup),
                typeof(Persistence.EntityFramework.PostgreSql.Startup),
                typeof(Persistence.MongoDb.Startup),
                typeof(Persistence.YesSql.SqliteStartup),
                typeof(Persistence.YesSql.SqlServerStartup),
                typeof(Persistence.YesSql.MySqlStartup),
                typeof(Persistence.YesSql.PostgreSqlStartup),
                typeof(Elsa.Server.Hangfire.Startup),
                typeof(Elsa.Scripting.JavaScript.Startup),
                typeof(Elsa.Activities.Webhooks.Startup),
                typeof(Webhooks.Persistence.EntityFramework.Sqlite.Startup),
                typeof(Webhooks.Persistence.EntityFramework.SqlServer.Startup),
                typeof(Webhooks.Persistence.EntityFramework.MySql.Startup),
                typeof(Webhooks.Persistence.EntityFramework.PostgreSql.Startup),
                typeof(Webhooks.Persistence.MongoDb.Startup),
                typeof(Webhooks.Persistence.YesSql.SqliteStartup),
                typeof(Webhooks.Persistence.YesSql.SqlServerStartup),
                typeof(Webhooks.Persistence.YesSql.MySqlStartup),
                typeof(Webhooks.Persistence.YesSql.PostgreSqlStartup),
                typeof(WorkflowSettings.Persistence.EntityFramework.Sqlite.Startup),
                typeof(WorkflowSettings.Persistence.EntityFramework.SqlServer.Startup),
                typeof(WorkflowSettings.Persistence.EntityFramework.MySql.Startup),
                typeof(WorkflowSettings.Persistence.EntityFramework.PostgreSql.Startup),
                typeof(WorkflowSettings.Persistence.MongoDb.Startup),
                typeof(WorkflowSettings.Persistence.YesSql.SqliteStartup),
                typeof(WorkflowSettings.Persistence.YesSql.SqlServerStartup),
                typeof(WorkflowSettings.Persistence.YesSql.MySqlStartup),
                typeof(WorkflowSettings.Persistence.YesSql.PostgreSqlStartup),
            };

            services
                .AddElsa(elsa => elsa
                    .AddActivitiesFrom<Startup>()
                    .AddWorkflowsFrom<Startup>()
                    .AddFeatures(startups, Configuration)
                    .ConfigureWorkflowChannels(options => elsaSection.GetSection("WorkflowChannels").Bind(options))
                    
                    // Optionally opt-out of indexing workflows stored in the database.
                    // These will be indexed when published/unpublished/deleted, so no need to do it during startup.
                    // Unless you have existing workflow definitions in the DB for which no triggers have yet been created.
                    .ExcludeWorkflowProviderFromStartupIndexing<DatabaseWorkflowProvider>()
                )
                .AddRetentionServices(options => elsaSection.GetSection("Retention").Bind(options));

            // Elsa API endpoints.
            services
                .AddNotificationHandlersFrom<Startup>()
                .AddElsaApiEndpoints()
                .AddElsaSwagger();

            // Allow arbitrary client browser apps to access the API for demo purposes only.
            // In a production environment, make sure to allow only origins you trust.
            services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().WithExposedHeaders("Content-Disposition")));

            // Workflow Testing
            services.AddWorkflowTestingServices();
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
                .UseCors(cors => cors
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .SetIsOriginAllowed(_ => true)
                    .AllowCredentials())
                .UseElsaFeatures()
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    endpoints.MapWorkflowTestHub();
                });
        }

        private void AddHangfire(IServiceCollection services, string dbConnectionString)
        {
            services
                .AddHangfire(config => config
                    // Use same SQLite database as Elsa for storing jobs. 
                    .UseSQLiteStorage(dbConnectionString)
                    .UseSimpleAssemblyNameTypeSerializer()

                    // Elsa uses NodaTime primitives, so Hangfire needs to be able to serialize them.
                    .UseRecommendedSerializerSettings(settings => settings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb)))
                .AddHangfireServer((sp, options) =>
                {
                    // Bind settings from configuration.
                    Configuration.GetSection("Hangfire").Bind(options);
                });
        }
    }
}