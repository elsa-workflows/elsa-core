using CShells.Features;
using Elsa.Extensions;

namespace Elsa.Server.Web.Features;

[ShellFeature("ActivitiesAndWorkflows", DependsOn = ["Elsa"])]
public class ActivitiesAndWorkflowsFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.ConfigureElsa(elsa =>
        {
            elsa.AddActivitiesFrom<ActivitiesAndWorkflowsFeature>();
            elsa.AddWorkflowsFrom<ActivitiesAndWorkflowsFeature>();
        });
    }
}