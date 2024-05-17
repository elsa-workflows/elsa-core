using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowActivities;

public class SaveWorkflowTests(App app) : AppComponentTest(app)
{
    private readonly IServiceScope _scope = app.Cluster.Pod1.Services.CreateScope();

    [Theory]
    [InlineData("Save1", true, true, true, true)]
    [InlineData("Save2", true, false, true, false)]
    [InlineData("Save3", false, true, false, false)]
    [InlineData("Save4", false, false, false, false)]
    private async Task SaveWorkflow(string name, bool usableAsActivity, bool publish, bool expectedInRegistry, bool isBrowsable)
    {
        var activityRegistry = _scope.ServiceProvider.GetRequiredService<IActivityRegistry>();
        
        var descriptor = activityRegistry.Find(name);
        if (descriptor is not null)
            activityRegistry.Remove(typeof(WorkflowDefinitionActivityProvider), descriptor);
        
        var importer = _scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionImporter>();
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
            Assert.NotNull(descriptor);
            Assert.Equal(isBrowsable, descriptor.IsBrowsable);
        }
        else
        {
            Assert.Null(descriptor);    
        }
    }

    protected override void OnDispose()
    {
        _scope.Dispose();
    }
}