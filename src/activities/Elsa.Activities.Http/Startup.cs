using Elsa.Attributes;
using Elsa.Services.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Http
{
    [Feature("Http")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            elsa.AddHttpActivities(configuration.GetSection("Elsa:Http").Bind);
        }
        
        public override void ConfigureApp(IApplicationBuilder app) => app.UseHttpActivities();
    }
}
