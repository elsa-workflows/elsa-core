using Elsa;
using Elsa.Attributes;
using Elsa.Services.Startup;
using ElsaDashboard.Samples.AspNetCore.Monolith.Workflows;
using Microsoft.Extensions.Configuration;

namespace ElsaDashboard.Samples.AspNetCore.Monolith
{
    [Feature("Workflows:Heartbeat")]
    public class HeartbeatWorkflowsFeature : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            elsa.AddWorkflow<HeartbeatWorkflow>();
        }
    }
}