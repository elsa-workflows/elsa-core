using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowActivities;

public class SaveWorkflowTests(App app) : AppComponentTest(app)
{
    [Test]
    [DisplayName("Saving workflows updates ActivityRegistry (name: $name, usableAsActivity: $usableAsActivity, publish: $publish, expectedInRegistry: $expectedInRegistry, isBrowsable: $isBrowsable)")]
    [Arguments("Save1", true, true, true, true)]
    [Arguments("Save2", true, false, false, false)]
    [Arguments("Save3", false, true, false, false)]
    [Arguments("Save4", false, false, false, false)]
    public async Task ActivityRegistry(string name, bool usableAsActivity, bool publish, bool expectedInRegistry, bool isBrowsable)
    {
        var activityRegistry = Scope.ServiceProvider.GetRequiredService<IActivityRegistry>();

        var descriptor = activityRegistry.Find(name);
        if (descriptor is not null)
            activityRegistry.Remove(typeof(WorkflowDefinitionActivityProvider), descriptor);

        var importer = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionImporter>();
        var request = new SaveWorkflowDefinitionRequest
        {
            Model = new WorkflowDefinitionModel
            {
                Name = name,
                DefinitionId = name,
                Options = new WorkflowOptions
                {
                    UsableAsActivity = usableAsActivity,
                    AutoUpdateConsumingWorkflows = true
                }
            },
            Publish = publish
        };
        await importer.ImportAsync(request);

        descriptor = activityRegistry.Find(name);

        if (expectedInRegistry)
        {
            await Assert.That(descriptor).IsNotNull();
            await Assert.That(descriptor.IsBrowsable).IsEqualTo(isBrowsable);
        }
        else
        {
            await Assert.That(descriptor).IsNull();
        }
    }
}
