using System.Text.Json.Nodes;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.IntegrationTests;

public class AIWorkflowGroundingToolTests
{
    [Test]
    [DisplayName("Workflow grounding tools search and return graph summaries")]
    public async Task WorkflowGroundingToolsSearchAndReturnGraphSummaries()
    {
        var definition = new WorkflowDefinition
        {
            Id = "version-1",
            DefinitionId = "workflow-1",
            Name = "Order intake",
            Description = "Receives orders",
            Version = 1,
            IsLatest = true,
            MaterializerName = "Json",
            StringData = """{ "activities": [ { "id": "a1", "type": "Elsa.Http.HttpEndpoint" } ] }"""
        };
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddSingleton<IWorkflowDefinitionStore>(new TestWorkflowDefinitionStore(definition));
        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IAIToolRegistry>();

        using var searchTool = await registry.FindAsync("workflows.search", new AIToolQuery { ActorId = "user-1" });
        var searchResult = await searchTool!.ExecuteAsync(new AIToolExecutionContext
        {
            ActorId = "user-1",
            ConversationId = "conversation-1",
            Arguments = new JsonObject { ["query"] = "order" }
        });

        using var graphTool = await registry.FindAsync("workflows.getDefinitionGraph", new AIToolQuery { ActorId = "user-1" });
        var graphResult = await graphTool!.ExecuteAsync(new AIToolExecutionContext
        {
            ActorId = "user-1",
            ConversationId = "conversation-1",
            Arguments = new JsonObject { ["id"] = "version-1" }
        });

        await Assert.That(searchResult.Data["returned"]!.GetValue<int>()).IsEqualTo(1);
        var graph = graphResult.Data["items"]!.AsArray()[0]!.AsObject();
        await Assert.That(graph["activityCount"]!.GetValue<int>()).IsEqualTo(1);
        await Assert.That(graph["activityTypes"]!.AsArray()[0]!.GetValue<string>()).IsEqualTo("Elsa.Http.HttpEndpoint");
    }

    private class TestWorkflowDefinitionStore(params WorkflowDefinition[] definitions) : IWorkflowDefinitionStore
    {
        private readonly List<WorkflowDefinition> _definitions = definitions.ToList();

        public Task<WorkflowDefinition?> FindAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult(Apply(filter).FirstOrDefault());

        public Task<WorkflowDefinition?> FindAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
            FindAsync(filter, cancellationToken);

        public Task<Page<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
        {
            var items = Apply(filter).ToList();
            return Task.FromResult(Page.Of<WorkflowDefinition>(items, items.Count));
        }

        public Task<Page<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default) =>
            FindManyAsync(filter, pageArgs, cancellationToken);

        public Task<IEnumerable<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<WorkflowDefinition>>(Apply(filter).ToList());

        public Task<IEnumerable<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
            FindManyAsync(filter, cancellationToken);

        public Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
        {
            var items = Apply(filter).Select(WorkflowDefinitionSummary.FromDefinition).ToList();
            return Task.FromResult(Page.Of<WorkflowDefinitionSummary>(items, items.Count));
        }

        public Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default) =>
            FindSummariesAsync(filter, pageArgs, cancellationToken);

        public Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<WorkflowDefinitionSummary>>(Apply(filter).Select(WorkflowDefinitionSummary.FromDefinition).ToList());

        public Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
            FindSummariesAsync(filter, cancellationToken);

        public Task<WorkflowDefinition?> FindLastVersionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken) =>
            Task.FromResult(Apply(filter).OrderByDescending(x => x.Version).FirstOrDefault());

        public Task SaveAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
        {
            _definitions.RemoveAll(x => x.Id == definition.Id);
            _definitions.Add(definition);
            return Task.CompletedTask;
        }

        public Task<WorkflowDefinitionUpdateResult> TryUpdateLatestAsync(
            WorkflowDefinitionFilter filter,
            Func<WorkflowDefinition, bool> matchesExpected,
            Func<WorkflowDefinition, WorkflowDefinition> update,
            CancellationToken cancellationToken = default)
        {
            var current = Apply(filter).FirstOrDefault();

            if (current is null)
                return Task.FromResult(WorkflowDefinitionUpdateResult.NotFound());

            if (!matchesExpected(current))
                return Task.FromResult(WorkflowDefinitionUpdateResult.Conflict());

            var next = update(current);
            _definitions.RemoveAll(x => x.Id == current.Id || x.Id == next.Id);
            if (next.Id != current.Id)
            {
                current.IsLatest = false;
                _definitions.Add(current);
            }

            _definitions.Add(next);
            return Task.FromResult(WorkflowDefinitionUpdateResult.Updated(next));
        }

        public Task SaveManyAsync(IEnumerable<WorkflowDefinition> definitions, CancellationToken cancellationToken = default)
        {
            foreach (var definition in definitions)
                _definitions.Add(definition);
            return Task.CompletedTask;
        }

        public Task<long> DeleteAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default) => Task.FromResult(0L);
        public Task<bool> AnyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default) => Task.FromResult(Apply(filter).Any());
        public Task<long> CountDistinctAsync(CancellationToken cancellationToken = default) => Task.FromResult((long)_definitions.Select(x => x.DefinitionId).Distinct().Count());
        public Task<bool> GetIsNameUnique(string name, string? definitionId = null, CancellationToken cancellationToken = default) => Task.FromResult(!_definitions.Any(x => x.Name == name && x.DefinitionId != definitionId));

        private IEnumerable<WorkflowDefinition> Apply(WorkflowDefinitionFilter filter)
        {
            var query = _definitions.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(filter.Id))
                query = query.Where(x => x.Id == filter.Id);
            if (!string.IsNullOrWhiteSpace(filter.DefinitionId))
                query = query.Where(x => x.DefinitionId == filter.DefinitionId);
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                query = query.Where(x => (x.Name?.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) || x.DefinitionId.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase));
            return query;
        }
    }
}
