using Elsa.Common.Models;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Models;
using NSubstitute;

namespace Elsa.Workflows.Management.UnitTests.Services;

public class WorkflowReferenceUpdaterOriginalSourceTests
{
    private const string ElsaScriptSource = "workflow Consumer { WriteLine(\"from-elsascript\"); }";

    [Fact]
    public async Task UpdateWorkflowReferencesAsync_WhenElsaScriptConsumerIsUpdated_KeepsOriginalSourceAndStillMaterializes()
    {
        var target = CreateTargetDefinition();
        var consumer = CreateElsaScriptConsumer();
        var consumerGraph = CreateConsumerGraph(consumer, target);
        var updater = CreateUpdater(target, consumer, consumerGraph);

        var result = await updater.UpdateWorkflowReferencesAsync(target);

        var updated = Assert.Single(result.UpdatedWorkflows);
        Assert.Equal(ElsaScriptSource, updated.OriginalSource);
        Assert.Equal("ElsaScript", updated.MaterializerName);

        var materialized = await MaterializeAsElsaScriptAsync(updated);

        Assert.Equal(consumer.DefinitionId, materialized.Identity.DefinitionId);
        Assert.Equal(consumer.Id, materialized.Identity.Id);
    }

    private static WorkflowReferenceUpdater CreateUpdater(
        WorkflowDefinition target,
        WorkflowDefinition consumer,
        WorkflowGraph consumerGraph)
    {
        var publisher = Substitute.For<IWorkflowDefinitionPublisher>();
        var workflowDefinitionService = Substitute.For<IWorkflowDefinitionService>();
        var store = Substitute.For<IWorkflowDefinitionStore>();
        var graphBuilder = Substitute.For<IWorkflowReferenceGraphBuilder>();
        var serializer = Substitute.For<IApiSerializer>();

        graphBuilder.BuildGraphAsync(target.DefinitionId, Arg.Any<CancellationToken>())
            .Returns(new WorkflowReferenceGraph([target.DefinitionId], [new WorkflowReferenceEdge(consumer.DefinitionId, target.DefinitionId)]));

        workflowDefinitionService.FindWorkflowGraphsAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns([consumerGraph]);
        workflowDefinitionService.MaterializeWorkflowAsync(consumer, Arg.Any<CancellationToken>())
            .Returns(consumerGraph);

        store.FindManyAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns([target]);
        store.FindAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(consumer);

        publisher.GetDraftAsync(consumer.DefinitionId, VersionOptions.Latest, Arg.Any<CancellationToken>())
            .Returns(consumer);
        publisher.SaveDraftAsync(Arg.Any<WorkflowDefinition>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<WorkflowDefinition>());

        serializer.Serialize(Arg.Any<object>()).Returns("serialized-root");

        return new(
            publisher,
            workflowDefinitionService,
            store,
            graphBuilder,
            new WorkflowDefinitionActivityDescriptorFactory(),
            Substitute.For<IActivityRegistry>(),
            serializer);
    }

    private static WorkflowDefinition CreateTargetDefinition()
    {
        return new()
        {
            Id = "target-v2",
            DefinitionId = "target-def",
            Name = "Target",
            Version = 2,
            IsPublished = true,
            IsLatest = true,
            MaterializerName = JsonWorkflowMaterializer.MaterializerName,
            Options = new WorkflowOptions
            {
                UsableAsActivity = true,
                AutoUpdateConsumingWorkflows = true
            }
        };
    }

    private static WorkflowDefinition CreateElsaScriptConsumer()
    {
        return new()
        {
            Id = "consumer-v1",
            DefinitionId = "consumer-def",
            Name = "Consumer",
            Version = 1,
            IsPublished = false,
            IsLatest = true,
            MaterializerName = "ElsaScript",
            OriginalSource = ElsaScriptSource,
            StringData = null
        };
    }

    private static WorkflowGraph CreateConsumerGraph(WorkflowDefinition consumer, WorkflowDefinition target)
    {
        var referencedActivity = new WorkflowDefinitionActivity
        {
            Id = "ref-activity",
            WorkflowDefinitionId = target.DefinitionId,
            WorkflowDefinitionVersionId = "target-v1",
            Version = 1
        };
        var workflow = new Workflow
        {
            Id = "consumer-workflow",
            Identity = new(consumer.DefinitionId, consumer.Version, consumer.Id),
            Publication = new(consumer.IsLatest, consumer.IsPublished),
            Root = referencedActivity
        };
        var rootNode = new ActivityNode(workflow, "Root");
        var childNode = new ActivityNode(referencedActivity, "Root");
        rootNode.AddChild(childNode);

        return new(workflow, rootNode, [rootNode, childNode]);
    }

    /// <summary>
    /// Mirrors <c>ElsaScriptWorkflowMaterializer</c>: compile <see cref="WorkflowDefinition.OriginalSource"/> only.
    /// </summary>
    private static Task<Workflow> MaterializeAsElsaScriptAsync(WorkflowDefinition definition)
    {
        var source = definition.OriginalSource ?? string.Empty;
        Assert.False(string.IsNullOrEmpty(source), "ElsaScript materializer would compile empty source.");

        return Task.FromResult(new Workflow
        {
            Identity = new(definition.DefinitionId, definition.Version, definition.Id, definition.TenantId)
        });
    }
}
