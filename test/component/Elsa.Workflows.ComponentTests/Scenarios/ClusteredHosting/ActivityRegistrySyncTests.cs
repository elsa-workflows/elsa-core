using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.ClusteredHosting;

public class ActivityRegistrySyncTests(App app) : AppComponentTest(app)
{
    private readonly IServiceScope _pod1Scope = app.Cluster.Pod1.Services.CreateScope();

    [Fact]
    public async Task ImportWorkflowActivity_ShouldUpdateOtherPods()
    {
        var client1 = Cluster.Pod1.CreateClient();
        var client2 = Cluster.Pod2.CreateClient();
        var client3 = Cluster.Pod3.CreateClient();
        
        var importer = _pod1Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionImporter>();
        var request = new SaveWorkflowDefinitionRequest
        {
            Model = new WorkflowDefinitionModel
            {
                Name = "Sub",
                Options = new WorkflowOptions
                {
                    UsableAsActivity = true,
                    AutoUpdateConsumingWorkflows = true
                }
            },
            Publish = true
        };
        await importer.ImportAsync(request);
    }

    public void Dispose()
    {
        _pod1Scope.Dispose();
    }
}