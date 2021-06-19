using Elsa.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Http
{
    [Feature("Http")]
    public class HttpStartup
    {
        public void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            elsa.AddHttpActivities(configuration.GetSection("Elsa:Http").Bind);
        }
        
        public void ConfigureApp(IApplicationBuilder app) => app.UseHttpActivities();
    }
}
