using Elsa.Common.Multitenancy;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Stimuli;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowDefinitionStorePopulation;

/// <summary>
/// Represents a test class for integration testing various scenarios related to the
/// `WorkflowDefinitionStorePopulation`. This class primarily focuses
/// on ensuring correct behavior when workflow definitions are published or updated, and their effects
/// on consuming workflows and triggers.
/// </summary>
public class Tests
{
    private readonly IServiceProvider _services;
    private readonly Workflow _shiftyWorkflow;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _shiftyWorkflow = new()
        {
            Identity = new(
                DefinitionId: "WorkflowWithTrigger",
                Version: 1,
                Id: "1",
                TenantId: Tenant.DefaultTenantId
            ),
            Root = new Event("Foo")
            {
                CanStartWorkflow = true
            }
        };

        _services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureServices(services => services
                .AddScoped<IWorkflowsProvider>(_ => new InMemoryWorkflowsProvider(_shiftyWorkflow))
                .AddScoped<IWorkflowMaterializer>(_ => new InMemoryWorkflowMaterializer(_shiftyWorkflow)))
            .Build();
    }

    /// <summary>
    /// When a dependency workflow is published, all consuming workflows are updated to point to the new version of the dependency.
    /// </summary>
    [Fact(DisplayName = "When a workflow definition from a given source has a different Id than the one in the store, the trigger should still point to the workflow definition version ID in the store.")]
    public async Task Test1()
    {
        // Initial population of the store from workflow providers.
        await _services.PopulateRegistriesAsync();
        
        // Artificially change the workflow definition version ID.
        _shiftyWorkflow.Identity = _shiftyWorkflow.Identity with
        {
            Id = ":1"
        };
        
        // Emulate reloading of workflow definitions.
        await _services.PopulateRegistriesAsync();

        // Triggering the workflow should still work.
        var stimulusSender = _services.GetRequiredService<IStimulusSender>();
        var stimulus = new EventStimulus("Foo");
        var triggerName = ActivityTypeNameHelper.GenerateTypeName<Event>();
        var result = await stimulusSender.SendAsync(triggerName, stimulus);

        Assert.NotEmpty(result.WorkflowInstanceResponses);
    }
}