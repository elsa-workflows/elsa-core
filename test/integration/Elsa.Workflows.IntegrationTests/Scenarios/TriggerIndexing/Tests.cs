using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
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
        _services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureServices(services =>
            {
                services.AddSingleton<IWorkflowMaterializer, FailingMaterializer>();
                services.AddSingleton<IWorkflowMaterializer>(sp => new WorkingMaterializer(_workflowRegistry));
            })
            .Build();

        _triggerIndexer = _services.GetRequiredService<ITriggerIndexer>();
        _triggerStore = _services.GetRequiredService<ITriggerStore>();
        _workflowDefinitionStore = _services.GetRequiredService<IWorkflowDefinitionStore>();
    }

    [Fact(DisplayName = "DeleteTriggersAsync should continue deleting triggers even when workflow fails to load")]
    public async Task DeleteTriggersAsync_WorkflowFailsToLoad_ContinuesWithOtherTriggers()
    {
        await _services.PopulateRegistriesAsync();

        // Arrange - Create workflow definitions that will all fail to load
        var workflows = new[]
        {
            CreateWorkflowDefinition("workflow1", "def1", "Json"),
            CreateWorkflowDefinition("workflow2", "def2", FailingMaterializer.MaterializerName),
            CreateWorkflowDefinition("workflow3", "def3", "Json")
        };

        var triggers = new[]
        {
            CreateTrigger("trigger1", "def1", "workflow1", "act1"),
            CreateTrigger("trigger2", "def2", "workflow2", "act2"),
            CreateTrigger("trigger3", "def3", "workflow3", "act3")
        };

        await SaveWorkflowsAndTriggersAsync(workflows, triggers);

        // Act - Delete triggers (all workflows will fail to load)
        await _triggerIndexer.DeleteTriggersAsync(new());

        // Assert - All triggers remain because all workflows failed to load
        var remainingTriggers = await _triggerStore.FindManyAsync(new());
        var triggerIds = remainingTriggers.Select(t => t.Id).OrderBy(id => id).ToList();

        Assert.Equal(3, remainingTriggers.Count());
        Assert.Equal(new[] { "trigger1", "trigger2", "trigger3" }, triggerIds);
    }

    [Fact(DisplayName = "DeleteTriggersAsync processes all workflows even when one fails")]
    public async Task DeleteTriggersAsync_OneWorkflowFails_ProcessesAllWorkflows()
    {
        await _services.PopulateRegistriesAsync();

        // Arrange - Register valid workflows in the registry
        _workflowRegistry["workflow1"] = new()
            { Identity = new("def1", 1, "workflow1") };
        _workflowRegistry["workflow3"] = new()
            { Identity = new("def3", 1, "workflow3") };

        var workflows = new[]
        {
            CreateWorkflowDefinition("workflow1", "def1", WorkingMaterializer.MaterializerName),
            CreateWorkflowDefinition("workflow2", "def2", FailingMaterializer.MaterializerName),
            CreateWorkflowDefinition("workflow3", "def3", WorkingMaterializer.MaterializerName)
        };

        var triggers = new[]
        {
            CreateTrigger("trigger1", "def1", "workflow1", "act1"),
            CreateTrigger("trigger2", "def2", "workflow2", "act2"),
            CreateTrigger("trigger3", "def3", "workflow3", "act3")
        };

        await SaveWorkflowsAndTriggersAsync(workflows, triggers);

        // Act - Delete triggers (workflow2 will fail, but workflow1 and workflow3 should succeed)
        await _triggerIndexer.DeleteTriggersAsync(new());

        // Assert - Only trigger2 remains since workflow2 failed to load
        var remainingTriggers = await _triggerStore.FindManyAsync(new());

        Assert.Single(remainingTriggers);
        Assert.Equal("trigger2", remainingTriggers.First().Id);
    }

    private static WorkflowDefinition CreateWorkflowDefinition(string id, string definitionId, string materializerName)
    {
        return new()
        {
            Id = id,
            DefinitionId = definitionId,
            Version = 1,
            IsPublished = true,
            IsLatest = true,
            MaterializerName = materializerName,
            StringData = "{\"root\":{\"type\":\"Elsa.Sequence\",\"id\":\"seq1\",\"version\":1,\"activities\":[]}}"
        };
    }

    private static StoredTrigger CreateTrigger(string id, string definitionId, string versionId, string activityId)
    {
        return new()
        {
            Id = id,
            WorkflowDefinitionId = definitionId,
            WorkflowDefinitionVersionId = versionId,
            Name = "TestTrigger",
            ActivityId = activityId
        };
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
}

/// <summary>
/// A custom materializer that throws an exception when materialization is attempted.
/// This simulates the "Provider not found" scenario.
/// </summary>
file class FailingMaterializer : IWorkflowMaterializer
{
    public const string MaterializerName = "FailingMaterializer";
    public string Name => MaterializerName;

    public ValueTask<Workflow> MaterializeAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default) =>
        ValueTask.FromException<Workflow>(new("Provider not found"));
}

/// <summary>
/// A custom materializer that successfully materializes workflows from a registry.
/// </summary>
file class WorkingMaterializer(Dictionary<string, Workflow> workflowRegistry) : IWorkflowMaterializer
{
    public const string MaterializerName = "WorkingMaterializer";
    public string Name => MaterializerName;

    public ValueTask<Workflow> MaterializeAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        if (workflowRegistry.TryGetValue(definition.Id, out var workflow))
            return ValueTask.FromResult(workflow);

        throw new InvalidOperationException($"Workflow {definition.Id} not found in registry");
    }
}
