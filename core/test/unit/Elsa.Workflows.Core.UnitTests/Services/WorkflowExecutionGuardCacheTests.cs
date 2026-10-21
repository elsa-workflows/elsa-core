using System.Collections;
using System.Reflection;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Compensation;
using Elsa.Workflows.Models;
using Elsa.Workflows.Pipelines.WorkflowExecution;

namespace Elsa.Workflows.Core.UnitTests.Services;

public class WorkflowExecutionGuardCacheTests : IAsyncLifetime
{
    private ActivityExecutionContext _activityContext = null!;
    private WorkflowExecutionContext _context = null!;

    public async Task InitializeAsync()
    {
        _activityContext = await new ActivityTestFixture(new WriteLine("guard-cache")).BuildAsync();
        _context = _activityContext.WorkflowExecutionContext;
    }

    public async Task DisposeAsync() => await ((IAsyncDisposable)_context.ServiceProvider).DisposeAsync();

    [Fact]
    public async Task GuardRechecksNodeMutationsAndSupportsTheAdvertisedCollectionOperations()
    {
        var graph = _context.WorkflowGraph;
        var nodes = graph.Nodes;
        var originalNodes = nodes.ToArray();
        var scopeNode = new ActivityNode(new CompensationScope(), "test");
        var pipeline = CreatePipeline();

        try
        {
            await RunAsync(pipeline);
            Assert.True(nodes.Contains(originalNodes[0]));
            Assert.Equal(originalNodes.Length, nodes.Count);
            var copiedNodes = new ActivityNode[nodes.Count];
            nodes.CopyTo(copiedNodes, 0);
            Assert.Equal(originalNodes, copiedNodes);
            Assert.IsAssignableFrom<IList<ActivityNode>>(nodes);
            Assert.IsAssignableFrom<IList>(nodes);

            nodes.Add(scopeNode);
            await AssertDraftOnlyAsync(pipeline);
            Assert.True(nodes.Remove(scopeNode));
            await RunAsync(pipeline);

            var list = (IList<ActivityNode>)nodes;
            var replacedIndex = 1;
            var previousNode = list[replacedIndex];
            list.RemoveAt(replacedIndex);
            list.Insert(replacedIndex, scopeNode);
            Assert.Equal(originalNodes.Length, nodes.Count);
            await AssertDraftOnlyAsync(pipeline);
            list.RemoveAt(replacedIndex);
            list.Insert(replacedIndex, previousNode);
            await RunAsync(pipeline);

            nodes.Clear();
            nodes.Add(scopeNode);
            await AssertDraftOnlyAsync(pipeline);
            nodes.Clear();
            foreach (var node in originalNodes)
                nodes.Add(node);
            await RunAsync(pipeline);

            list[replacedIndex] = scopeNode;
            await AssertDraftOnlyAsync(pipeline);
            list[replacedIndex] = previousNode;
            await RunAsync(pipeline);

            var nonGenericList = (IList)nodes;
            var appendedNode = new ActivityNode(new WriteLine("list"), "test");
            var appendedIndex = nonGenericList.Add(appendedNode);
            Assert.True(nonGenericList.Contains(appendedNode));
            nonGenericList.RemoveAt(appendedIndex);

            // Collection<T> remains backed by a List<T>, so active enumeration still detects mutation.
            var enumerator = nodes.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            nodes.Add(scopeNode);
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            nodes.Remove(scopeNode);
            await RunAsync(pipeline);
        }
        finally
        {
            nodes.Clear();
            foreach (var node in originalNodes)
                nodes.Add(node);
        }
    }

    [Fact]
    public async Task GuardRechecksActivityTypeAndOpaqueDiscriminatorMutations()
    {
        var graph = _context.WorkflowGraph;
        var node = graph.Nodes.First(x => x.Activity is not Sequence);
        var activity = (Activity)node.Activity;
        var originalType = activity.Type;
        var pipeline = CreatePipeline();

        await RunAsync(pipeline);
        activity.Type = CompensationScope.ActivityTypeName;
        await AssertDraftOnlyAsync(pipeline);
        activity.Type = originalType;
        await RunAsync(pipeline);

        var notFound = new NotFoundActivity(missingTypeName: "Elsa.UnknownActivity")
        {
            OriginalActivityJson = "{\"type\":\"Elsa.UnknownActivity\"}"
        };
        var opaqueNode = AddGraphActivity(notFound);
        try
        {
            await RunAsync(pipeline);
            notFound.MissingTypeName = CompensationScope.ActivityTypeName;
            await AssertDraftOnlyAsync(pipeline);
            notFound.MissingTypeName = "Elsa.UnknownActivity";
            await RunAsync(pipeline);

            notFound.OriginalActivityJson = "{\"type\":\"Elsa.UnknownActivity\",\"originalActivityJson\":\"{\\\"type\\\":\\\"Elsa.CompensationScope\\\"}\"}";
            await AssertDraftOnlyAsync(pipeline);
            notFound.OriginalActivityJson = "{\"type\":\"Elsa.UnknownActivity\"}";
            await RunAsync(pipeline);

            notFound.OriginalActivityJson = "{\"type\":\"Elsa.CompensationScope\"}";
            await AssertDraftOnlyAsync(pipeline);
            notFound.OriginalActivityJson = "{\"type\":\"Elsa.UnknownActivity\"}";
            await RunAsync(pipeline);
        }
        finally
        {
            graph.Nodes.Remove(opaqueNode);
        }
    }

    [Fact]
    public async Task GuardDoesNotCachePassesForUntrackedActivityTypeImplementations()
    {
        var pipeline = CreatePipeline();
        await AssertUntrackedActivityRemainsObservedAsync(pipeline, new DirectActivity());
        await AssertUntrackedActivityRemainsObservedAsync(pipeline, new ReimplementedTypeActivity());
    }

    [Fact]
    public async Task SaturatedNodeRevisionFallsBackToScanning()
    {
        var graph = _context.WorkflowGraph;
        var nodes = graph.Nodes;
        var scope = new CompensationScope();
        var pipeline = CreatePipeline();

        await RunAsync(pipeline);

        var revision = nodes.GetType().GetProperty("Revision", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(nodes)!;
        var valueField = revision.GetType().GetField("_value", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var disabledField = revision.GetType().GetField("_disabled", BindingFlags.Instance | BindingFlags.NonPublic)!;
        valueField.SetValue(revision, long.MaxValue);

        nodes.Add(new ActivityNode(scope, "test"));

        Assert.Equal(1, (int)disabledField.GetValue(revision)!);
        await AssertDraftOnlyAsync(pipeline);
    }

    private async Task AssertUntrackedActivityRemainsObservedAsync(WorkflowExecutionPipeline pipeline, IActivity activity)
    {
        var node = AddGraphActivity(activity);
        try
        {
            await RunAsync(pipeline);
            var typeReadsAfterFirstPass = GetTypeReadCount(activity);
            await RunAsync(pipeline);
            Assert.True(GetTypeReadCount(activity) > typeReadsAfterFirstPass);

            activity.Type = CompensationScope.ActivityTypeName;
            await AssertDraftOnlyAsync(pipeline);
        }
        finally
        {
            _context.WorkflowGraph.Nodes.Remove(node);
        }

        await RunAsync(pipeline);
    }

    private static int GetTypeReadCount(IActivity activity) => activity switch
    {
        DirectActivity direct => direct.TypeGetterReads,
        ReimplementedTypeActivity derived => derived.TypeGetterReads,
        _ => throw new ArgumentOutOfRangeException(nameof(activity))
    };

    private ActivityNode AddGraphActivity(IActivity activity)
    {
        var node = new ActivityNode(activity, "test");
        _context.WorkflowGraph.Nodes.Add(node);
        return node;
    }

    private WorkflowExecutionPipeline CreatePipeline() => new(_context.ServiceProvider, _ => { });

    private Task RunAsync(WorkflowExecutionPipeline pipeline) => pipeline.Pipeline(_context).AsTask();

    private async Task AssertDraftOnlyAsync(WorkflowExecutionPipeline pipeline)
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.Pipeline(_context).AsTask());
        Assert.Equal(CompensationScope.DraftOnlyRefusalMessage, exception.Message);
    }

    private sealed class DirectActivity : IActivity
    {
        private string _type = "Elsa.Test.DirectActivity";

        public string Id { get; set; } = "direct";
        public string NodeId { get; set; } = "direct";
        public string? Name { get; set; }
        public string Type
        {
            get
            {
                TypeGetterReads++;
                return _type;
            }
            set => _type = value;
        }
        public int TypeGetterReads { get; private set; }
        public int Version { get; set; } = 1;
        public IDictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();
        public IDictionary<string, object> SyntheticProperties { get; set; } = new Dictionary<string, object>();
        public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context) => ValueTask.FromResult(true);
        public ValueTask ExecuteAsync(ActivityExecutionContext context) => ValueTask.CompletedTask;
    }

    private sealed class ReimplementedTypeActivity : CodeActivity, IActivity
    {
        private string _type = "Elsa.Test.ReimplementedTypeActivity";

        public int TypeGetterReads { get; private set; }

        string IActivity.Type
        {
            get
            {
                TypeGetterReads++;
                return _type;
            }
            set => _type = value;
        }

        protected override void Execute(ActivityExecutionContext context)
        {
        }
    }
}
