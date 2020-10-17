using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using YesSql.Provider.Sqlite;

namespace Elsa.Samples.CustomActivities
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddElsa(option => option.UsePersistence(db => db.UseSqLite("Data Source=elsa.db;Cache=Shared")))
                .AddHttpActivities()
                .AddActivity<ReadQueryString>()
                .AddWorkflow<EchoQueryStringWorkflow>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseHttpActivities();
        }
    }
}