using Elsa.Mediator.Configuration;
using Elsa.ServiceConfiguration.Abstractions;
using Elsa.ServiceConfiguration.Attributes;
using Elsa.ServiceConfiguration.Services;
using Elsa.Workflows.Core.Configuration;
using Elsa.Workflows.Management.Configuration;
using Elsa.Workflows.Persistence.Configuration;
using Elsa.Workflows.Runtime.Configuration;

namespace Elsa.Configuration;

[Dependency(typeof(MediatorConfigurator))]
[Dependency(typeof(WorkflowsConfigurator))]
[Dependency(typeof(WorkflowPersistenceConfigurator))]
[Dependency(typeof(WorkflowRuntimeConfigurator))]
[Dependency(typeof(WorkflowManagementConfigurator))]
public class ElsaConfigurator : ConfiguratorBase
{
    public ElsaConfigurator(IServiceConfiguration serviceConfiguration) : base(serviceConfiguration)
    {
    }
}