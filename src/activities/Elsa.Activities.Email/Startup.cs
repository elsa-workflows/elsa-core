using Elsa.Attributes;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Email
{
    [Feature("Email")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            elsa.AddEmailActivities(options => configuration.GetSection("Elsa:Smtp").Bind(options));
        }
    }
}