using Elsa.Attributes;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Console
{
    [Feature("Console")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            elsa.AddConsoleActivities();
        }
    }
}
