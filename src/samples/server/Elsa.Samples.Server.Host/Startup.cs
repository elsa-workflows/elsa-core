using Elsa.Activities.Conductor.Extensions;
using Elsa.Activities.UserTask.Extensions;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;
using Elsa.Providers.WorkflowStorage;
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
            var sqlServerConnectionString = Configuration.GetConnectionString("SqlServer");
            var sqliteConnectionString = Configuration.GetConnectionString("Sqlite");
            var mongoDbConnectionString = Configuration.GetConnectionString("MongoDb");

            services.AddControllers();

            services
                .AddActivityPropertyOptionsProvider<VehicleActivity>()
                .AddRuntimeSelectItemsProvider<VehicleActivity>()
                //.AddRedis(Configuration.GetConnectionString("Redis"))
                .AddElsa(elsa => elsa
                    .WithContainerName(Configuration["ContainerName"] ?? System.Environment.MachineName)
                    .UseEntityFrameworkPersistence(ef => ef.UseSqlite(sqliteConnectionString))
                    //.UseMongoDbPersistence(options => options.ConnectionString = mongoDbConnectionString)
                    //.UseYesSqlPersistence(config => config.UsePostgreSql("Server=localhost;Port=5432;Database=yessql5;User Id=root;Password=Password12!;"))
                    //.UseRabbitMq(Configuration.GetConnectionString("RabbitMq"))
                    //.UseRebusCacheSignal()
                    //.UseRedisCacheSignal()
                    .UseDefaultWorkflowStorageProvider<TransientWorkflowStorageProvider>()
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
                    .AddConductorActivities(options => elsaSection.GetSection("Conductor").Bind(options))
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
                .UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}