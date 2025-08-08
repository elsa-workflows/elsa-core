using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.ComponentTests.WorkflowProviders;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ResumeWorkflowAfterHostRestart;

public class Tests(ITestOutputHelper testOutputHelper)
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private MemoryStore<string> _dbWorkflowDefinitionsStore = new();
    private readonly MemoryStore<WorkflowInstance> _testWorkflowInstanceStore = new();

    private IServiceProvider CreateTestServicesScope()
    {
        return new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .ConfigureServices(services=>
            {
                services.AddWorkflowDefinitionProvider<TestWorkflowProvider>();
            })
            .ConfigureElsa(elsa => elsa
                .UseWorkflowManagement()
                .UseWorkflows(workflows => workflows
                    .WithStandardOutStreamProvider(_ => new StandardOutStreamProvider(new XunitConsoleTextWriter(testOutputHelper)))
                )
            )
            .Build();
    }

    [Theory(DisplayName = "MaterializeWorkflow")]
    [InlineData("materialize-issue-workflow")]
    public async Task MaterializeWorkflowPriorToPopulateRegistriesAsync( string fileName)
    {
        var jsonContent = await File.ReadAllTextAsync(@$"Scenarios/ResumeWorkflowAfterHostRestart/{fileName}.json");

        var host = CreateTestServicesScope();
        using var scope= host.CreateScope();
        
        // await host.PopulateRegistriesAsync();

        var testWorkflowProvider = (TestWorkflowProvider)scope.ServiceProvider.GetServices<IWorkflowsProvider>().First( it=> it.Name == "Test");

        var activitySerializer = scope.ServiceProvider.GetRequiredService<IActivitySerializer>();
        var mapper = scope.ServiceProvider.GetRequiredService<WorkflowDefinitionMapper>();

        var workflowDefinitionModel = activitySerializer.Deserialize<WorkflowDefinitionModel>(jsonContent);
        var workflow = mapper.Map(workflowDefinitionModel);

        var materializedWorkflow = new MaterializedWorkflow(workflow, "Test", JsonWorkflowMaterializer.MaterializerName);
            
        testWorkflowProvider.MaterializedWorkflows.Add(materializedWorkflow);
    }
}
