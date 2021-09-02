using Elsa.Activities.UserTask.Extensions;
using Elsa.Attributes;
using Elsa.Options;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;

namespace Elsa.Activities.UserTask
{
    [Feature("UserTask")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            elsa.AddUserTaskActivities();
        }
    }
}
