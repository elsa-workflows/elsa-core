using Elsa.Attributes;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scripting.JavaScript
{
    [Feature("JavaScript:Activities")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            elsa.AddJavaScriptActivities();
        }
    }
}
