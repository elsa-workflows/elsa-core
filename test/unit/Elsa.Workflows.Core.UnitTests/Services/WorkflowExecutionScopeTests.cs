using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;

namespace Elsa.Workflows.Core.UnitTests.Services;

public class WorkflowExecutionScopeTests : IAsyncLifetime
{
    private WorkflowExecutionContext _context = null!;

    public async Task InitializeAsync() => _context = (await new ActivityTestFixture(new WriteLine("test")).BuildAsync()).WorkflowExecutionContext;
    public async Task DisposeAsync() => await ((IAsyncDisposable)_context.ServiceProvider).DisposeAsync();

    [Fact]
    public void NestedOwnershipReusesResourceAndOnlyOuterScopeReleasesIt()
    {
        var resource = new TrackedResource();
        using var owner = WorkflowExecutionScope.Begin(_context);
        using (var nested = WorkflowExecutionScope.Begin(_context))
        {
            Assert.Same(resource, nested.GetOrAddResource("test", () => resource));
        }
        Assert.Equal(0, resource.DisposeCount);
        Assert.Same(resource, owner.GetOrAddResource<TrackedResource>("test", () => throw new InvalidOperationException("Resource recreated")));

        owner.Dispose();
        owner.Dispose();
        Assert.Equal(1, resource.DisposeCount);
        Assert.False(_context.TransientProperties.ContainsKey("test"));
    }

    [Fact]
    public void UnboundRegistrationIsRejectedBeforeFactoryAndDisposedScopesCannotRegister()
    {
        using var bridge = WorkflowExecutionScope.Begin(_context.Id);
        var factoryCalls = 0;
        Assert.Throws<InvalidOperationException>(() => bridge.GetOrAddResource("test", () =>
        {
            factoryCalls++;
            return new TrackedResource();
        }));
        Assert.Equal(0, factoryCalls);
        var nested = WorkflowExecutionScope.Begin(_context);
        nested.Dispose();
        Assert.Throws<ObjectDisposedException>(() => nested.GetOrAddResource("test", () => new TrackedResource()));
    }

    [Fact]
    public void NestedBridgesRetainOuterOwnershipAndRestoreItsAmbientScope()
    {
        var resource = new TrackedResource();
        using var owner = WorkflowExecutionScope.Begin(_context.Id);
        using (WorkflowExecutionScope.Begin(_context.Id))
        using (var contextScope = WorkflowExecutionScope.Begin(_context))
            contextScope.GetOrAddResource("test", () => resource);
        Assert.Equal(0, resource.DisposeCount);
        using (var restored = WorkflowExecutionScope.Begin(_context))
            Assert.Same(resource, restored.GetOrAddResource("test", () => new TrackedResource()));
        Assert.Equal(0, resource.DisposeCount);
        owner.Dispose();
        Assert.Equal(1, resource.DisposeCount);
    }

    [Fact]
    public async Task BridgeBindsOnlyFirstMatchingContextEvenWhenInstanceIdsMatch()
    {
        var firstResource = new TrackedResource();
        var secondResource = new TrackedResource();
        var other = await WorkflowExecutionContext.CreateAsync(_context.ServiceProvider, _context.WorkflowGraph, _context.Id);
        using var bridge = WorkflowExecutionScope.Begin(_context.Id);
        using (var first = WorkflowExecutionScope.Begin(_context))
            first.GetOrAddResource("test", () => firstResource);
        using (var second = WorkflowExecutionScope.Begin(other))
            second.GetOrAddResource("test", () => secondResource);

        Assert.Equal(0, firstResource.DisposeCount);
        Assert.Equal(1, secondResource.DisposeCount);
        bridge.Dispose();
        Assert.Equal(1, firstResource.DisposeCount);
    }

    [Fact]
    public void SequentialReuseStartsANewLifetime()
    {
        var firstResource = new TrackedResource();
        using (var first = WorkflowExecutionScope.Begin(_context))
            first.GetOrAddResource("test", () => firstResource);
        var secondResource = new TrackedResource();
        using (var second = WorkflowExecutionScope.Begin(_context))
        {
            Assert.Same(secondResource, second.GetOrAddResource("test", () => secondResource));
            Assert.Equal(0, secondResource.DisposeCount);
        }
        Assert.Equal(1, firstResource.DisposeCount);
        Assert.Equal(1, secondResource.DisposeCount);
    }

    [Fact]
    public async Task ConcurrentChildrenHaveSeparateLifetimesAndDoNotReleaseParent()
    {
        var parentResource = new TrackedResource();
        using var owner = WorkflowExecutionScope.Begin(_context);
        owner.GetOrAddResource("test", () => parentResource);
        var childrenReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseChildren = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = 0;
        var resources = new[] { new TrackedResource(), new TrackedResource() };
        var tasks = resources.Select(resource => Task.Run(async () =>
        {
            var child = await WorkflowExecutionContext.CreateAsync(_context.ServiceProvider, _context.WorkflowGraph, _context.Id);
            using var childOwner = WorkflowExecutionScope.Begin(child);
            Assert.Same(resource, childOwner.GetOrAddResource("test", () => resource));
            if (Interlocked.Increment(ref started) == resources.Length)
                childrenReady.SetResult();
            await releaseChildren.Task;
        })).ToArray();

        try
        {
            await childrenReady.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.All(resources, resource => Assert.Equal(0, resource.DisposeCount));
            Assert.Equal(0, parentResource.DisposeCount);
        }
        finally
        {
            releaseChildren.TrySetResult();
            await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(5));
        }
        Assert.All(resources, resource => Assert.Equal(1, resource.DisposeCount));
        Assert.Equal(0, parentResource.DisposeCount);
    }

    [Fact]
    public async Task ChildOutlivingParentDoesNotResurrectDisposedAmbientBridge()
    {
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var continueChild = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resource = new TrackedResource();
        using var parent = WorkflowExecutionScope.Begin(_context.Id);
        var childTask = Task.Run(async () =>
        {
            ready.SetResult();
            await continueChild.Task;
            var child = await WorkflowExecutionContext.CreateAsync(_context.ServiceProvider, _context.WorkflowGraph, _context.Id);
            using var owner = WorkflowExecutionScope.Begin(child);
            owner.GetOrAddResource("test", () => resource);
        });
        try
        {
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(5));
            parent.Dispose();
        }
        finally
        {
            continueChild.TrySetResult();
            await childTask.WaitAsync(TimeSpan.FromSeconds(5));
        }
        Assert.Equal(1, resource.DisposeCount);
    }

    private sealed class TrackedResource : IDisposable
    {
        public int DisposeCount { get; private set; }
        public void Dispose() => DisposeCount++;
    }
}
