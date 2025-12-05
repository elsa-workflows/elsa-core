using CShells.Features;
using Elsa.Extensions;

namespace Elsa.Server.Web.Features;

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