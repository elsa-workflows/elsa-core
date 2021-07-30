using System.Collections.Generic;
using Elsa.Server.Hangfire.Extensions;
using Hangfire;
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
            };

            services
                .AddElsa(elsa => elsa
                    .AddActivitiesFrom<Startup>()
                    .AddWorkflowsFrom<Startup>()
                    .AddFeatures(startups, Configuration)
                    .ConfigureWorkflowChannels(options => elsaSection.GetSection("WorkflowChannels").Bind(options))
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
                .UseElsaFeatures()
                .UseRouting()
                .UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}