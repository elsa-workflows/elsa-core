using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.FaultyWorkflows
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddElsa(elsa => elsa
                    .UseEntityFrameworkPersistence(options => options.UseSqlite("Data Source=elsa.db;Cache=Shared;", db => db.MigrationsAssembly(typeof(SqliteElsaContextFactory).Assembly.GetName().Name)))
                    .AddHttpActivities()
                    .AddWorkflow<FaultyWorkflow>());
        }

        public void Configure(IApplicationBuilder app)
        {
            // Add HTTP activities middleware.
            app.UseHttpActivities();
        }
    }
}