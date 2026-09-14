using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.ClusteredHosting;

public class ActivityRegistrySyncTests(App app) : AppComponentTest(app)
{
    [Test]
    [Skip("Not yet implemented")]
    public async Task ImportWorkflowActivity_ShouldUpdateOtherPods()
    {
        await using var pod1Scope = Cluster.Pod1.Services.CreateAsyncScope();
        await using var pod2Scope = (await Cluster.GetPod2Async()).Services.CreateAsyncScope();
        await using var pod3Scope = (await Cluster.GetPod3Async()).Services.CreateAsyncScope();
        var pod1ActivityRegistry = pod1Scope.ServiceProvider.GetRequiredService<IActivityRegistry>();
        var pod2ActivityRegistry = pod2Scope.ServiceProvider.GetRequiredService<IActivityRegistry>();
        var pod3ActivityRegistry = pod3Scope.ServiceProvider.GetRequiredService<IActivityRegistry>();
        
        var sub1Descriptor = pod1ActivityRegistry.Find("Sub");
        await Assert.That(sub1Descriptor).IsNull();

        var importer = pod1Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionImporter>();
        var request = new SaveWorkflowDefinitionRequest
        {
            Model = new WorkflowDefinitionModel
            {
                Name = "Sub",
                DefinitionId = "Sub",
                Options = new WorkflowOptions
                {
                    UsableAsActivity = true,
                    AutoUpdateConsumingWorkflows = true
                }
            },
            Publish = true
        };
        await importer.ImportAsync(request);

        sub1Descriptor = pod1ActivityRegistry.Find("Sub");
        await Assert.That(sub1Descriptor).IsNotNull();

        sub1Descriptor = pod2ActivityRegistry.Find("Sub");
        await Assert.That(sub1Descriptor).IsNotNull();

        sub1Descriptor = pod3ActivityRegistry.Find("Sub");
        await Assert.That(sub1Descriptor).IsNotNull();
    }

}
