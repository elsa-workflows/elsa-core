using Elsa.Runtime;
using Elsa.Server.GraphQL.Extensions;
using Elsa.StartupTasks;
using GraphQL.Server.Ui.GraphiQL;
using GraphQL.Server.Ui.Playground;
using GraphQL.Server.Ui.Voyager;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

            services
                .AddElsa(elsa => elsa
                    .UseMongoDbWorkflowStores("ElsaServer", Configuration.GetConnectionString("MongoDb")));

            services
                .AddGraphQL(options => Configuration.GetSection("GraphQL").Bind(options))
                .AddConsoleActivities()
                .AddHttp(options => options.Bind(elsaSection.GetSection("Http")))
                .AddEmail(options => options.Bind(elsaSection.GetSection("Smtp")))
                .AddTimerActivities(options => options.Bind(elsaSection.GetSection("BackgroundRunner")))
                .AddElsaManagement()
                .AddStartupTask<ResumeRunningWorkflowsTask>();

            EnableSynchronousIO(services);
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpActivities();
            app.UseElsaGraphQL();
            app.UseGraphQLPlayground(new GraphQLPlaygroundOptions());
            app.UseGraphiQLServer(new GraphiQLOptions());
            app.UseGraphQLVoyager(new GraphQLVoyagerOptions());
        }

        private void EnableSynchronousIO(IServiceCollection services)
        {
            // Workaround until GraphQL can swap off Newtonsoft.Json and onto the new MS one.
            // Depending on whether you're using IIS or Kestrel, the code required is different.
            // See: https://github.com/graphql-dotnet/graphql-dotnet/issues/1116

            // kestrel
            services.Configure<KestrelServerOptions>(options => { options.AllowSynchronousIO = true; });

            // IIS
            services.Configure<IISServerOptions>(options => { options.AllowSynchronousIO = true; });
        }
    }
}