using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Multitenant;
using Elsa.Activities.Http.OpenApi;
using Elsa.Extensions;
using Elsa.HostedServices;
using Elsa.Models;
using Elsa.Multitenancy;
using Elsa.Multitenancy.Strategies;
using Elsa.Retention.Extensions;
using Elsa.Retention.Filters;
using Elsa.Retention.HostedServices;
using Elsa.Runtime;
using Elsa.Services;
using Elsa.Services.Startup;
using Elsa.WorkflowTesting.Api.Extensions;
using Hangfire;
using Hangfire.SQLite;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
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

            services.AddElsaServices();        

            // Elsa API endpoints.
            services
                .AddElsaApiEndpoints()
                .AddElsaSwagger(options => options.DocumentFilter<HttpEndpointDocumentFilter>());

            // Allow arbitrary client browser apps to access the API for demo purposes only.
            // In a production environment, make sure to allow only origins you trust.
            services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().WithExposedHeaders("Content-Disposition")));
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // This will all go in the ROOT CONTAINER and is NOT TENANT SPECIFIC.

            var services = new ServiceCollection();

            var elsaSection = Configuration.GetSection("Elsa");

            builder.ConfigureElsaServices(services, elsa => elsa
                    .AddActivitiesFrom<Startup>()
                    .AddWorkflowsFrom<Startup>()
                    .AddElsaMultitenancy()
                    .ConfigureWorkflowChannels(options => elsaSection.GetSection("WorkflowChannels").Bind(options))
                    .AddFeatures(_startups, Configuration));

            services
                // Optionally opt-out of indexing workflows stored in the database.
                // These will be indexed when published/unpublished/deleted, so no need to do it during startup.
                // Unless you have existing workflow definitions in the DB for which no triggers have yet been created.
                //.ExcludeWorkflowProviderFromStartupIndexing<DatabaseWorkflowProvider>()

                // For distributed hosting, configure Rebus with a real message broker such as RabbitMQ or Azure Service Bus.
                //.UseRabbitMq(Configuration.GetConnectionString("RabbitMq"))

                // When testing a distributed on your local machine, make sure each instance has a unique "container" name.
                // This name is used to create unique input queues for pub/sub messaging where the competing consumer pattern is undesirable in order to deliver a message to each subscriber.
                //.WithContainerName(Configuration.GetValue<string>("ContainerName") ?? System.Environment.MachineName)
                .AddRetentionServices(builder, options =>
                {
                    // Bind options from configuration.
                    elsaSection.GetSection("Retention").Bind(options);

                    // Configure a custom filter pipeline that deletes completed AND faulted workflows.
                    options.ConfigurePipeline = pipeline => pipeline
                        .AddFilter(new WorkflowStatusFilter(WorkflowStatus.Cancelled, WorkflowStatus.Faulted, WorkflowStatus.Finished))
                        // Could add additional filters. For example, if there's a way to know that some workflow is a child workflow, maybe don't delete the parent.
                        ;
                });

            builder.Populate(services);

            builder.AddNotificationHandlersFrom<Startup>();

            // Workflow Testing
            builder.AddWorkflowTestingServices();
        }

        public void Configure(IApplicationBuilder app)
        {
            var multitenancyEnabled = Configuration.GetIsMultitenancyEnabled();

            // tODO: move to middleware
            if (!multitenancyEnabled)
            {
                app.Use(async (context, next) =>
                {
                    //tODO: read default prefix from constant
                    context.Request.Path = $"/default{context.Request.Path}";

                    await next(context);
                });
            }

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
                    endpoints.MapWorkflowTestHub(Configuration);
                });
        }

        public static MultitenantContainer ConfigureMultitenantContainer(IContainer container)
        {
            return MultitenantContainerFactory.CreateMultitenantContainer(container);
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

        // TODO: Determine startup types based on project references, similar to Orchard Core's Targets.props for Applications and Modules.
        // Note that simply loading all referenced assemblies will not include assemblies where no types have been referenced in this project (due to assembly trimming?).
        private Type[] _startups => new[]
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
                typeof(Elsa.Activities.Sql.Startup),
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
                typeof(Secrets.Persistence.EntityFramework.Sqlite.Startup),
                typeof(Secrets.Persistence.MongoDb.Startup)
            };
    }
}