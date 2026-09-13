using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.UnitTests.Extensions;

public class ServiceCollectionWorkflowExtensionsTests
{
    [Test]
    public async Task AddWorkflow_PostConfiguresRuntimeOptions()
    {
        var services = new ServiceCollection();
        var configuredWorkflows = new Dictionary<string, Func<IServiceProvider, ValueTask<IWorkflow>>>();

        services.Configure<RuntimeOptions>(options => options.Workflows = configuredWorkflows);
        services.AddWorkflow<ServiceCollectionRegisteredWorkflow>();

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<RuntimeOptions>>().Value;
        var workflowKey = typeof(ServiceCollectionRegisteredWorkflow).GetSimpleAssemblyQualifiedName();

        await Assert.That(options.Workflows).IsSameReferenceAs(configuredWorkflows);
        await Assert.That(options.Workflows.Keys).Contains(workflowKey);
    }
}

public class ServiceCollectionRegisteredWorkflow : WorkflowBase;
