using System.Text.Json.Nodes;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.IntegrationTests;

public class AIRuntimeGroundingToolTests
{
    [Fact(DisplayName = "Runtime grounding tools return redacted incident evidence")]
    public async Task RuntimeGroundingToolsReturnRedactedIncidentEvidence()
    {
        var instance = new WorkflowInstance
        {
            Id = "instance-1",
            DefinitionId = "workflow-1",
            DefinitionVersionId = "version-1",
            Version = 1,
            Status = WorkflowStatus.Finished,
            SubStatus = WorkflowSubStatus.Faulted,
            IncidentCount = 1,
            WorkflowState = new WorkflowState
            {
                Incidents =
                {
                    new ActivityIncident("activity-1", "node-1", "Elsa.Http.HttpEndpoint", "API key password leaked", null, DateTimeOffset.UtcNow)
                },
                Input = new Dictionary<string, object> { ["password"] = "secret" }
            }
        };
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddSingleton<IWorkflowInstanceStore>(new TestWorkflowInstanceStore(instance));
        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IAIToolRegistry>();

        using var tool = await registry.FindAsync("incidents.search", new AIToolQuery { ActorId = "user-1" });
        var result = await tool!.ExecuteAsync(new AIToolExecutionContext
        {
            ActorId = "user-1",
            ConversationId = "conversation-1",
            Arguments = new JsonObject { ["definitionId"] = "workflow-1" }
        });

        Assert.Equal(1, result.Data["returned"]!.GetValue<int>());
        var incident = result.Data["items"]!.AsArray()[0]!.AsObject();
        Assert.Equal("instance-1", incident["workflowInstanceId"]!.GetValue<string>());
        Assert.DoesNotContain("secret", result.Data.ToJsonString(), StringComparison.OrdinalIgnoreCase);
    }

    private class TestWorkflowInstanceStore(params WorkflowInstance[] instances) : IWorkflowInstanceStore
    {
        private readonly List<WorkflowInstance> _instances = instances.ToList();

        public ValueTask<WorkflowInstance?> FindAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Apply(filter).FirstOrDefault());

        public ValueTask<Page<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
        {
            var items = Apply(filter).ToList();
            return ValueTask.FromResult(Page.Of<WorkflowInstance>(items, items.Count));
        }

        public ValueTask<Page<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
            FindManyAsync(filter, pageArgs, cancellationToken);

        public ValueTask<IEnumerable<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IEnumerable<WorkflowInstance>>(Apply(filter).ToList());

        public ValueTask<IEnumerable<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
            FindManyAsync(filter, cancellationToken);

        public ValueTask<long> CountAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult((long)Apply(filter).Count());

        public ValueTask<Page<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
        {
            var items = Apply(filter).Select(WorkflowInstanceSummary.FromInstance).ToList();
            return ValueTask.FromResult(Page.Of<WorkflowInstanceSummary>(items, items.Count));
        }

        public ValueTask<Page<WorkflowInstanceSummary>> SummarizeManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
            SummarizeManyAsync(filter, pageArgs, cancellationToken);

        public ValueTask<IEnumerable<string>> FindManyIdsAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IEnumerable<string>>(Apply(filter).Select(x => x.Id).ToList());

        public ValueTask<Page<string>> FindManyIdsAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
        {
            var ids = Apply(filter).Select(x => x.Id).ToList();
            return ValueTask.FromResult(Page.Of<string>(ids, ids.Count));
        }

        public ValueTask<Page<string>> FindManyIdsAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
            FindManyIdsAsync(filter, pageArgs, cancellationToken);

        public ValueTask<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IEnumerable<WorkflowInstanceSummary>>(Apply(filter).Select(WorkflowInstanceSummary.FromInstance).ToList());

        public ValueTask<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync<TOrder>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrder> order, CancellationToken cancellationToken = default) =>
            SummarizeManyAsync(filter, cancellationToken);

        public ValueTask SaveAsync(WorkflowInstance instance, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask AddAsync(WorkflowInstance instance, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask UpdateAsync(WorkflowInstance instance, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SaveManyAsync(IEnumerable<WorkflowInstance> instances, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask<long> DeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) => ValueTask.FromResult(0L);
        public Task UpdateUpdatedTimestampAsync(string workflowInstanceId, DateTimeOffset value, CancellationToken cancellationToken = default) => Task.CompletedTask;

        private IEnumerable<WorkflowInstance> Apply(WorkflowInstanceFilter filter)
        {
            var query = _instances.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(filter.Id))
                query = query.Where(x => x.Id == filter.Id);
            if (!string.IsNullOrWhiteSpace(filter.DefinitionId))
                query = query.Where(x => x.DefinitionId == filter.DefinitionId);
            if (filter.HasIncidents != null)
                query = query.Where(x => filter.HasIncidents == true ? x.IncidentCount > 0 : x.IncidentCount == 0);
            return query;
        }
    }
}
