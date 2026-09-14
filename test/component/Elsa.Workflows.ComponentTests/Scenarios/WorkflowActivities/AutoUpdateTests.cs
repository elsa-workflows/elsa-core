using Elsa.Http;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.Services;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowActivities;

public class AutoUpdateTests : AppComponentTest
{
    private const string ParentDefinitionVersionId = "4b584e249fdca951";
    private const string ParentDefinitionId = "878770f04439a55d";
    private const string ChildDefinitionId = "f353742a9ef6af4";

    private static readonly object HttpChangeTokenSignal = new();
    private static readonly object TriggerChangeTokenSignal = new();
    private static readonly object GraphChangeTokenSignal = new();
    private IMemoryCache _cache = null!;
    private TriggerChangeTokenSignalEvents _changeTokenEvents = null!;
    private IWorkflowDefinitionCacheManager _definitionCacheManager = null!;
    private IHasher _hasher = null!;
    private IHttpWorkflowsCacheManager _httpCacheManager = null!;
    private IWorkflowDefinitionPublisher _publisher = null!;
    private SignalManager _signalManager = null!;
    private IWorkflowDefinitionCacheManager _workflowCacheManager = null!;
    public AutoUpdateTests(App app) : base(app)
    {
    }

    protected override ValueTask OnInitializeAsync()
    {
        _cache = Scope.ServiceProvider.GetRequiredService<IMemoryCache>();
        _hasher = Scope.ServiceProvider.GetRequiredService<IHasher>();
        _definitionCacheManager = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionCacheManager>();
        _publisher = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionPublisher>();

        _httpCacheManager = Scope.ServiceProvider.GetRequiredService<IHttpWorkflowsCacheManager>();
        _workflowCacheManager = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionCacheManager>();

        _signalManager = Scope.ServiceProvider.GetRequiredService<SignalManager>();
        _changeTokenEvents = Scope.ServiceProvider.GetRequiredService<TriggerChangeTokenSignalEvents>();
        return ValueTask.CompletedTask;
    }

    [Test]
    [DisplayName("Updating a workflow with `auto update consuming workflows` should invalidate consuming workflows from cache")]
    public async Task UpdateWorkflowWithAutoUpdate()
    {
        // Run workflow to make sure the all required items for running the workflow are in the cache.
        var client = WorkflowServer.CreateHttpWorkflowClient();
        await client.GetStringAsync("test-cache-invalidation");

        // Make sure the items are in the cache.
        var hash = _httpCacheManager.ComputeBookmarkHash("/test-cache-invalidation", "get");
        await Assert.That(_cache.TryGetValue($"http-workflow:{hash}", out _)).IsTrue();

        var filter = new TriggerFilter
        {
            Hash = hash
        };
        var hashedFilter = _hasher.Hash(filter);
        await Assert.That(_cache.TryGetValue($"IEnumerable`1:{hashedFilter}", out _)).IsTrue();

        var parentWorkflowDefinitionFilter = new WorkflowDefinitionFilter
        {
            Id = ParentDefinitionVersionId
        };
        var parentDefinitionCacheKey = _definitionCacheManager.CreateWorkflowDefinitionFilterCacheKey(parentWorkflowDefinitionFilter);
        var parentGraphCacheKey = _definitionCacheManager.CreateWorkflowVersionCacheKey(ParentDefinitionVersionId);
        await Assert.That(_cache.TryGetValue(parentDefinitionCacheKey, out _)).IsTrue();
        await Assert.That(_cache.TryGetValue(parentGraphCacheKey, out _)).IsTrue();

        // Set change tokens.
        var httpChangeToken = _workflowCacheManager.CreateWorkflowDefinitionChangeTokenKey(ParentDefinitionId);
        var triggerChangeToken = _httpCacheManager.GetTriggerChangeTokenKey(hash);
        var graphChangeToken = _workflowCacheManager.CreateWorkflowDefinitionChangeTokenKey(ParentDefinitionId);

        void OnChangeTokenSignalTriggered(object? sender, TriggerChangeTokenSignalEventArgs args)
        {
            if (args.Key == httpChangeToken) _signalManager.Trigger(HttpChangeTokenSignal, args);
            if (args.Key == triggerChangeToken) _signalManager.Trigger(TriggerChangeTokenSignal, args);
            if (args.Key == graphChangeToken) _signalManager.Trigger(GraphChangeTokenSignal, args);
        }

        _changeTokenEvents.ChangeTokenSignalTriggered += OnChangeTokenSignalTriggered;
        try
        {
            // (Act) Save the draft version of the child workflow and update the references.
            await _publisher.PublishAsync(ChildDefinitionId);

            // Wait until the notifications for updating the cache have been sent and check the cache.
            await _signalManager.WaitAsync<TriggerChangeTokenSignalEventArgs>(HttpChangeTokenSignal);
            await _signalManager.WaitAsync<TriggerChangeTokenSignalEventArgs>(TriggerChangeTokenSignal);
            await _signalManager.WaitAsync<TriggerChangeTokenSignalEventArgs>(GraphChangeTokenSignal);

            await Assert.That(_cache.TryGetValue($"http-workflow:{hash}", out _)).IsFalse();
            await Assert.That(_cache.TryGetValue($"IEnumerable`1:{hashedFilter}", out _)).IsFalse();
            await Assert.That(_cache.TryGetValue(parentDefinitionCacheKey, out _)).IsFalse();
            await Assert.That(_cache.TryGetValue(parentGraphCacheKey, out _)).IsFalse();
        }
        finally
        {
            _changeTokenEvents.ChangeTokenSignalTriggered -= OnChangeTokenSignalTriggered;
        }
    }
}
