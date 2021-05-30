using Elsa.Activities.UserTask.Extensions;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;
<<<<<<< Updated upstream
=======
//using Elsa.Persistence.MongoDb.Extensions;
using Elsa.Persistence.YesSql;
using Elsa.Rebus.RabbitMq;
>>>>>>> Stashed changes
using Elsa.Samples.Server.Host.Activities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
<<<<<<< Updated upstream
=======
using MongoDB.Bson.Serialization;
//using YesSql.Provider.PostgreSql;
>>>>>>> Stashed changes

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
                .AddElsa(elsa => elsa
<<<<<<< Updated upstream
                    .UseEntityFrameworkPersistence(ef => ef.UseSqlite())
=======
                    .WithContainerName(Configuration["ContainerName"] ?? System.Environment.MachineName)
                    //.UseEntityFrameworkPersistence(ef => ef.UseSqlite(sqliteConnectionString))
                    //.UseMongoDbPersistence(options => options.ConnectionString = mongoDbConnectionString)
                    //.UseYesSqlPersistence(config => config.UsePostgreSql("Server=localhost;Port=5432;Database=yessql5;User Id=root;Password=Password12!;"))
                    //.UseRabbitMq(Configuration.GetConnectionString("RabbitMq"))
                    .UseRebusCacheSignal()
                    //.UseRedisCacheSignal()
>>>>>>> Stashed changes
                    .AddConsoleActivities()
                    .AddHttpActivities(elsaSection.GetSection("Http").Bind)
                    .AddEmailActivities(elsaSection.GetSection("Smtp").Bind)
                    .AddQuartzTemporalActivities()
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