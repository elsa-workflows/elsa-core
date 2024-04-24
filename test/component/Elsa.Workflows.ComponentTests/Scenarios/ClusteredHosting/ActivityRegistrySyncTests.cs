using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.ClusteredHosting;

public class ActivityRegistrySyncTests(App app) : AppComponentTest(app)
{
    private readonly IServiceScope _pod1Scope = app.Cluster.Pod1.Services.CreateScope();
    private readonly IServiceScope _pod2Scope = app.Cluster.Pod2.Services.CreateScope();
    private readonly IServiceScope _pod3Scope = app.Cluster.Pod3.Services.CreateScope();

    [Fact]
    public async Task ImportWorkflowActivity_ShouldUpdateOtherPods()
    {
        var pod1ActivityRegistry = _pod1Scope.ServiceProvider.GetRequiredService<IActivityRegistry>();
        var pod2ActivityRegistry = _pod2Scope.ServiceProvider.GetRequiredService<IActivityRegistry>();
        var pod3ActivityRegistry = _pod3Scope.ServiceProvider.GetRequiredService<IActivityRegistry>();
        
        var sub1Descriptor = pod1ActivityRegistry.Find("Sub");
        Assert.Null(sub1Descriptor);
        
        var importer = _pod1Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionImporter>();
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
        Assert.NotNull(sub1Descriptor);
        
        sub1Descriptor = pod2ActivityRegistry.Find("Sub");
        Assert.NotNull(sub1Descriptor);
        
        sub1Descriptor = pod3ActivityRegistry.Find("Sub");
        Assert.NotNull(sub1Descriptor);
    }

    protected override void OnDispose()
    {
        _pod1Scope.Dispose();
        _pod2Scope.Dispose();
        _pod3Scope.Dispose();
    }
}