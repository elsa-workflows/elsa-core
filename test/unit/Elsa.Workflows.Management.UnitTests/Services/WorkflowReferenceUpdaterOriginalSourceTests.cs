using Elsa.Common.Models;
using Elsa.Dsl.ElsaScript.Contracts;
using Elsa.Dsl.ElsaScript.Materializers;
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
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Workflows.Management.UnitTests.Services;

public class WorkflowReferenceUpdaterOriginalSourceTests
{
    private const string ElsaScriptSource = "workflow Consumer { WriteLine(\"from-elsascript\"); }";

    [Fact]
    public async Task UpdateWorkflowReferencesAsync_SkipsElsaScriptConsumerAndUpdatesJsonConsumer()
    {
        var target = CreateTargetDefinition();
        var elsaScriptConsumer = CreateElsaScriptConsumer();
        var jsonConsumer = CreateJsonConsumer();
        var elsaScriptGraph = CreateConsumerGraph(elsaScriptConsumer, target, "elsa-consumer-workflow");
        var jsonGraph = CreateConsumerGraph(jsonConsumer, target, "json-consumer-workflow");
        var logger = new CollectingLogger();
        var publisher = Substitute.For<IWorkflowDefinitionPublisher>();
        var updater = CreateUpdater(target, [elsaScriptConsumer, jsonConsumer], [elsaScriptGraph, jsonGraph], publisher, logger);

        var result = await updater.UpdateWorkflowReferencesAsync(target);

        var updated = Assert.Single(result.UpdatedWorkflows);
        Assert.Equal(jsonConsumer.DefinitionId, updated.DefinitionId);
        Assert.Equal("serialized-root", updated.StringData);
        Assert.Null(updated.OriginalSource);
        Assert.Equal(JsonWorkflowMaterializer.MaterializerName, updated.MaterializerName);

        Assert.Equal(ElsaScriptSource, elsaScriptConsumer.OriginalSource);
        Assert.Null(elsaScriptConsumer.StringData);
        Assert.Equal("ElsaScript", elsaScriptConsumer.MaterializerName);
        Assert.Equal("consumer-elsascript-v1", elsaScriptConsumer.Id);

        var compiler = Substitute.For<IElsaScriptCompiler>();
        compiler.CompileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Workflow());
        var materializer = new ElsaScriptWorkflowMaterializer(compiler);
        await materializer.MaterializeAsync(elsaScriptConsumer, CancellationToken.None);
        await compiler.Received(1).CompileAsync(ElsaScriptSource, Arg.Any<CancellationToken>());

        await publisher.DidNotReceive().SaveDraftAsync(elsaScriptConsumer, Arg.Any<CancellationToken>());
        await publisher.DidNotReceive().PublishAsync(elsaScriptConsumer, Arg.Any<CancellationToken>());
        await publisher.Received(1).SaveDraftAsync(jsonConsumer, Arg.Any<CancellationToken>());
        Assert.Contains(logger.Messages, message =>
            message.Contains(elsaScriptConsumer.DefinitionId, StringComparison.Ordinal)
            && message.Contains(target.DefinitionId, StringComparison.Ordinal)
            && message.Contains(target.Version.ToString(), StringComparison.Ordinal)
            && message.Contains("must be updated manually", StringComparison.Ordinal));
    }

    private static WorkflowReferenceUpdater CreateUpdater(
        WorkflowDefinition target,
        IReadOnlyCollection<WorkflowDefinition> consumers,
        IReadOnlyCollection<WorkflowGraph> consumerGraphs,
        IWorkflowDefinitionPublisher publisher,
        ILogger<WorkflowReferenceUpdater> logger)
    {
        var workflowDefinitionService = Substitute.For<IWorkflowDefinitionService>();
        var store = Substitute.For<IWorkflowDefinitionStore>();
        var graphBuilder = Substitute.For<IWorkflowReferenceGraphBuilder>();
        var serializer = Substitute.For<IApiSerializer>();
        var consumersById = consumers.ToDictionary(c => c.DefinitionId);

        graphBuilder.BuildGraphAsync(target.DefinitionId, Arg.Any<CancellationToken>())
            .Returns(new WorkflowReferenceGraph(
                [target.DefinitionId],
                consumers.Select(c => new WorkflowReferenceEdge(c.DefinitionId, target.DefinitionId)).ToList()));

        workflowDefinitionService.FindWorkflowGraphsAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(consumerGraphs);

        foreach (var (consumer, graph) in consumers.Zip(consumerGraphs))
        {
            workflowDefinitionService.MaterializeWorkflowAsync(consumer, Arg.Any<CancellationToken>())
                .Returns(graph);
            publisher.GetDraftAsync(consumer.DefinitionId, VersionOptions.Latest, Arg.Any<CancellationToken>())
                .Returns(consumer);
        }

        store.FindManyAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns([target]);
        store.FindAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var filter = call.Arg<WorkflowDefinitionFilter>();
                return filter.DefinitionId != null && consumersById.TryGetValue(filter.DefinitionId, out var consumer)
                    ? consumer
                    : consumers.First();
            });

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
            serializer,
            logger);
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
            Id = "consumer-elsascript-v1",
            DefinitionId = "consumer-elsascript",
            Name = "ElsaScript Consumer",
            Version = 1,
            IsPublished = false,
            IsLatest = true,
            MaterializerName = "ElsaScript",
            OriginalSource = ElsaScriptSource,
            StringData = null
        };
    }

    private static WorkflowDefinition CreateJsonConsumer()
    {
        return new()
        {
            Id = "consumer-json-v1",
            DefinitionId = "consumer-json",
            Name = "Json Consumer",
            Version = 1,
            IsPublished = false,
            IsLatest = true,
            MaterializerName = JsonWorkflowMaterializer.MaterializerName,
            OriginalSource = "FILE_JSON",
            StringData = "OLD_JSON"
        };
    }

    private static WorkflowGraph CreateConsumerGraph(WorkflowDefinition consumer, WorkflowDefinition target, string workflowId)
    {
        var referencedActivity = new WorkflowDefinitionActivity
        {
            Id = $"{workflowId}-ref",
            WorkflowDefinitionId = target.DefinitionId,
            WorkflowDefinitionVersionId = "target-v1",
            Version = 1
        };
        var workflow = new Workflow
        {
            Id = workflowId,
            Identity = new(consumer.DefinitionId, consumer.Version, consumer.Id),
            Publication = new(consumer.IsLatest, consumer.IsPublished),
            Root = referencedActivity
        };
        var rootNode = new ActivityNode(workflow, "Root");
        var childNode = new ActivityNode(referencedActivity, "Root");
        rootNode.AddChild(childNode);

        return new(workflow, rootNode, [rootNode, childNode]);
    }

    private sealed class CollectingLogger : ILogger<WorkflowReferenceUpdater>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
