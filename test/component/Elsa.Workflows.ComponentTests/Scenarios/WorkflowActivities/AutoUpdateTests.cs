using Elsa.Http;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.Services;
using Elsa.Workflows.ComponentTests.Helpers;
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
    private readonly IMemoryCache _cache;
    private readonly TriggerChangeTokenSignalEvents _changeTokenEvents;
    private readonly IWorkflowDefinitionCacheManager _definitionCacheManager;
    private readonly IHasher _hasher;
    private readonly IHttpWorkflowsCacheManager _httpCacheManager;
    private readonly IWorkflowDefinitionPublisher _publisher;
    private readonly SignalManager _signalManager;
    private readonly IWorkflowDefinitionCacheManager _workflowCacheManager;
    private string? _graphChangeToken;

    private string? _httpChangeToken;
    private string? _triggerChangeToken;

    public AutoUpdateTests(App app) : base(app)
    {
        _cache = Scope.ServiceProvider.GetRequiredService<IMemoryCache>();
        _hasher = Scope.ServiceProvider.GetRequiredService<IHasher>();
        _definitionCacheManager = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionCacheManager>();
        _publisher = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionPublisher>();

        _httpCacheManager = Scope.ServiceProvider.GetRequiredService<IHttpWorkflowsCacheManager>();
        _workflowCacheManager = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionCacheManager>();

        _signalManager = Scope.ServiceProvider.GetRequiredService<SignalManager>();
        _changeTokenEvents = Scope.ServiceProvider.GetRequiredService<TriggerChangeTokenSignalEvents>();
        _changeTokenEvents.ChangeTokenSignalTriggered += OnChangeTokenSignalTriggered;
    }

    [Fact(DisplayName = "Updating a workflow with `auto update consuming workflows` should invalidate consuming workflows from cache")]
    public async Task UpdateWorkflowWithAutoUpdate()
    {
        // Run workflow to make sure the all required items for running the workflow are in the cache.
        var client = WorkflowServer.CreateHttpWorkflowClient();
        await client.GetStringAsync("test-cache-invalidation");

        // Make sure the items are in the cache.
        var hash = _httpCacheManager.ComputeBookmarkHash("/test-cache-invalidation", "get");
        Assert.True(_cache.TryGetValue($"http-workflow:{hash}", out _));

        var filter = new TriggerFilter
        {
            Hash = hash
        };
        var hashedFilter = _hasher.Hash(filter);
        Assert.True(_cache.TryGetValue($"IEnumerable`1:{hashedFilter}", out _));

        var parentWorkflowDefinitionFilter = new WorkflowDefinitionFilter
        {
            Id = ParentDefinitionVersionId
        };
        var parentVersionCacheKey = _definitionCacheManager.CreateWorkflowFilterCacheKey(parentWorkflowDefinitionFilter);
        Assert.True(_cache.TryGetValue(parentVersionCacheKey, out _));

        // Set change tokens.
        _httpChangeToken = _workflowCacheManager.CreateWorkflowDefinitionChangeTokenKey(ParentDefinitionId);
        _triggerChangeToken = _httpCacheManager.GetTriggerChangeTokenKey(hash);
        _graphChangeToken = _workflowCacheManager.CreateWorkflowDefinitionChangeTokenKey(ParentDefinitionId);

        // (Act) Save the draft version of the child workflow and update the references.
        await _publisher.PublishAsync(ChildDefinitionId);

        // Wait until the notifications for updating the cache have been send and check the cache.
        await _signalManager.WaitAsync<TriggerChangeTokenSignalEventArgs>(HttpChangeTokenSignal);
        await _signalManager.WaitAsync<TriggerChangeTokenSignalEventArgs>(TriggerChangeTokenSignal);
        await _signalManager.WaitAsync<TriggerChangeTokenSignalEventArgs>(GraphChangeTokenSignal);

        Assert.False(_cache.TryGetValue($"http-workflow:{hash}", out _));
        Assert.False(_cache.TryGetValue($"IEnumerable`1:{hashedFilter}", out _));
        Assert.False(_cache.TryGetValue(parentVersionCacheKey, out _));
    }

    private void OnChangeTokenSignalTriggered(object? sender, TriggerChangeTokenSignalEventArgs args)
    {
        if (args.Key == _httpChangeToken) _signalManager.Trigger(HttpChangeTokenSignal, args);
        if (args.Key == _triggerChangeToken) _signalManager.Trigger(TriggerChangeTokenSignal, args);
        if (args.Key == _graphChangeToken) _signalManager.Trigger(GraphChangeTokenSignal, args);
    }
}