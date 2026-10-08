using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.IntegrationTests.Scenarios.TriggerIndexing.Fakes;
using Elsa.Workflows.IntegrationTests.Scenarios.TriggerIndexing.TestData;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.TriggerIndexing;

public class Tests
{
    private readonly IServiceProvider _services;
    private readonly ITriggerIndexer _triggerIndexer;
    private readonly ITriggerStore _triggerStore;
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly Dictionary<string, Workflow> _workflowRegistry = new();

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).ConfigureServices(services =>
        {
            services.AddSingleton<IWorkflowMaterializer, FailingMaterializer>();
            services.AddSingleton<IWorkflowMaterializer>(_ => new WorkingMaterializer(_workflowRegistry));
        })
        .Build();

        _triggerIndexer = _services.GetRequiredService<ITriggerIndexer>();
        _triggerStore = _services.GetRequiredService<ITriggerStore>();
        _workflowDefinitionStore = _services.GetRequiredService<IWorkflowDefinitionStore>();
    }

    [Theory(DisplayName = "DeleteTriggersAsync handles workflow materialization failures correctly")]
    [MemberData(nameof(GetTriggerDeletionTestData))]
    public async Task DeleteTriggersAsync_HandlesWorkflowFailures(TriggerDeletionTestScenario scenario)
    {
        // Arrange
        var builder = new TriggerTestDataBuilder();

        foreach (var workflow in scenario.Workflows)
        {
            builder.AddWorkflow(workflow.Id, workflow.DefinitionId, workflow.MaterializerName);

            // Register working workflows in the registry
            if (workflow.ShouldSucceed)
                RegisterWorkflowInRegistry(workflow.Id, workflow.DefinitionId);
        }

        var testData = builder.Build();
        await SetupTestScenarioAsync(testData);

        // Act
        await DeleteTriggersAsync();

        // Assert
        await AssertRemainingTriggers(scenario.ExpectedRemainingTriggerIds);
    }

    public static IEnumerable<object[]> GetTriggerDeletionTestData()
    {
        // Scenario 1: All workflows fail to load - all triggers should remain
        yield return
        [
            new TriggerDeletionTestScenario
            {
                DisplayName = "All workflows fail to load",
                Workflows =
                [
                    new("workflow1", "def1", "Json", ShouldSucceed: false),
                    new("workflow2", "def2", FailingMaterializer.MaterializerName, ShouldSucceed: false),
                    new("workflow3", "def3", "Json", ShouldSucceed: false)
                ],
                ExpectedRemainingTriggerIds = ["trigger1", "trigger2", "trigger3"]
            }
        ];

        // Scenario 2: One workflow fails, others succeed - only the failing workflow's trigger remains
        yield return
        [
            new TriggerDeletionTestScenario
            {
                DisplayName = "One workflow fails, others succeed",
                Workflows =
                [
                    new("workflow1", "def1", WorkingMaterializer.MaterializerName, ShouldSucceed: true),
                    new("workflow2", "def2", FailingMaterializer.MaterializerName, ShouldSucceed: false),
                    new("workflow3", "def3", WorkingMaterializer.MaterializerName, ShouldSucceed: true)
                ],
                ExpectedRemainingTriggerIds = ["trigger2"]
            }
        ];
    }

    private async Task AssertRemainingTriggers(string[] expectedTriggerIds)
    {
        var remainingTriggerIds = await GetRemainingTriggerIdsAsync();
        Assert.Equal(expectedTriggerIds.OrderBy(id => id), remainingTriggerIds);
    }

    private async Task SaveWorkflowsAndTriggersAsync(WorkflowDefinition[] workflows, StoredTrigger[] triggers)
    {
        foreach (var workflow in workflows)
            await _workflowDefinitionStore.SaveAsync(workflow);

        foreach (var trigger in triggers)
            await _triggerStore.SaveAsync(trigger);

        var allTriggers = await _triggerStore.FindManyAsync(new());
        Assert.Equal(triggers.Length, allTriggers.Count());
    }


    private void RegisterWorkflowInRegistry(string workflowId, string definitionId)
    {
        _workflowRegistry[workflowId] = new()
        {
            Identity = new(definitionId, 1, workflowId)
        };
    }

    private async Task SetupTestScenarioAsync((WorkflowDefinition[] Workflows, StoredTrigger[] Triggers) testData)
    {
        await _services.PopulateRegistriesAsync();
        await SaveWorkflowsAndTriggersAsync(testData.Workflows, testData.Triggers);
    }

    private async Task DeleteTriggersAsync()
    {
        await _triggerIndexer.DeleteTriggersAsync(new());
    }

    private async Task<string[]> GetRemainingTriggerIdsAsync()
    {
        var remainingTriggers = await _triggerStore.FindManyAsync(new());
        return remainingTriggers.Select(t => t.Id).OrderBy(id => id).ToArray();
    }
}