using Elsa.Runtime;
using Elsa.Server.GraphQL.Extensions;
using Elsa.StartupTasks;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Voyager;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YesSql.Provider.Sqlite;

namespace Elsa.Server.Host
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
            var connectionString = Configuration.GetConnectionString("Sqlite");

            services
                .AddElsa(elsa => elsa
                    .UsePersistence(config => config.UseSqLite(connectionString)));

            services
                .AddElsaServer()
                .AddElsaGraphQL()
                
                .AddCors(options => options.AddDefaultPolicy(cors => cors
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()))
                
                .AddConsoleActivities()
                .AddHttpActivities(options => options.Bind(elsaSection.GetSection("Http")))
                .AddEmailActivities(options => options.Bind(elsaSection.GetSection("Smtp")))
                .AddTimerActivities(options => options.Bind(elsaSection.GetSection("BackgroundRunner")))
                .AddStartupTask<ResumeRunningWorkflowsTask>();
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app
                .UseCors()
                .UseGraphQL("/graphql")
                .UsePlayground("/graphql")
                .UseVoyager("/graphql")
                .UseHttpActivities();
        }
    }
}