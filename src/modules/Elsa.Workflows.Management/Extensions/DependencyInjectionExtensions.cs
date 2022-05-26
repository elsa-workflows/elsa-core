using Elsa.ServiceConfiguration.Services;
using Elsa.Workflows.Core.Configuration;
using Elsa.Workflows.Management.Configuration;

namespace Elsa.Workflows.Management.Extensions;

public static class DependencyInjectionExtensions
{
    public static WorkflowConfigurator UseManagement(this WorkflowConfigurator workflow, Action<WorkflowManagementConfigurator>? configure = default)
    {
        workflow.ServiceConfiguration.Configure(configure);
        return workflow;
    }
}