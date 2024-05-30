using Elsa.Common.Models;
using Elsa.Http;
using Elsa.Http.Bookmarks;
using Elsa.Http.Contracts;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowActivities;

public class AutoUpdateTests : AppComponentTest
{
    private readonly IMemoryCache _cache;
    private readonly IBookmarkHasher _bookmarkHasher;
    private readonly IHasher _hasher;
    private readonly IWorkflowDefinitionCacheManager _definitionCacheManager;
    private readonly IWorkflowDefinitionPublisher _publisher;
    private readonly ISignalManager _signalManager;
    private readonly ITriggerChangeTokenSignalEvents _changeTokenEvents;
    private readonly IWorkflowDefinitionManager _definitionManager;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    
    private readonly IHttpWorkflowsCacheManager _httpCacheManager;
    private readonly IWorkflowDefinitionCacheManager _workflowCacheManager;
    
    private string _httpChangeToken;
    private string _triggerChangeToken;
    private string _graphChangeToken;
    
    private static readonly object HttpChangeTokenSignal = new();
    private static readonly object TriggerChangeTokenSignal = new();
    private static readonly object GraphChangeTokenSignal = new();

    private const string ParentDefinitionVersionId = "4b584e249fdca951";
    private const string ParentDefinitionId = "878770f04439a55d";
    private const string ChildDefinitionId = "f353742a9ef6af4";

    public AutoUpdateTests(App app) : base(app)
    {
        _cache = Scope.ServiceProvider.GetRequiredService<IMemoryCache>();
        _bookmarkHasher = Scope.ServiceProvider.GetRequiredService<IBookmarkHasher>();
        _hasher = Scope.ServiceProvider.GetRequiredService<IHasher>();
        _definitionCacheManager = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionCacheManager>();
        _publisher = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionPublisher>();
        _definitionManager = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionManager>();
        _workflowDefinitionService = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionService>();
        
        _httpCacheManager = Scope.ServiceProvider.GetRequiredService<IHttpWorkflowsCacheManager>();
        _workflowCacheManager = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionCacheManager>();
        
        _signalManager = Scope.ServiceProvider.GetRequiredService<ISignalManager>();
        _changeTokenEvents = Scope.ServiceProvider.GetRequiredService<ITriggerChangeTokenSignalEvents>();
        _changeTokenEvents.ChangeTokenSignalTriggered += OnChangeTokenSignalTriggered;
    }

    [Fact(DisplayName = "Updating a workflow with `auto update consuming workflows` should invalidate consuming workflows from cache")]
    public async Task UpdateWorkflowWithAutoUpdate()
    {
        //Run workflow to make sure the all required items for running the workflow are in the cache
        var client = WorkflowServer.CreateHttpWorkflowClient();
        await client.GetStringAsync("test-cache-invalidation");
        
        //Make sure the items are in the cache
        var hash = _httpCacheManager.ComputeBookmarkHash("/test-cache-invalidation", "get");
        Assert.True(_cache.TryGetValue($"http-workflow:{hash}", out _));

        var filter = new TriggerFilter
        {
            Hash = hash
        };
        var hashedFilter = _hasher.Hash(filter);
        Assert.True(_cache.TryGetValue($"IEnumerable`1:{hashedFilter}", out _));

        var parentVersionCacheKey = _definitionCacheManager.CreateWorkflowVersionCacheKey(ParentDefinitionVersionId);
        Assert.True(_cache.TryGetValue(parentVersionCacheKey, out _));
        
        //Set change tokens
        _httpChangeToken = _workflowCacheManager.CreateWorkflowDefinitionChangeTokenKey(ParentDefinitionId);
        _triggerChangeToken = _httpCacheManager.GetTriggerChangeTokenKey(hash);
        _graphChangeToken = _workflowCacheManager.CreateWorkflowDefinitionChangeTokenKey(ParentDefinitionId);
        
        //(Act) Save the draft version of the child workflow and update the references 
        await _publisher.PublishAsync(ChildDefinitionId);

        var childDefinition = await _workflowDefinitionService.FindWorkflowDefinitionAsync(ChildDefinitionId, VersionOptions.Latest);
        await _definitionManager.UpdateReferencesInConsumingWorkflows(childDefinition!);
        
        //Wait till the notifications for updating the cache have been send and check the cache.
        await _signalManager.WaitAsync<TriggerChangeTokenSignalEventArgs>(HttpChangeTokenSignal);
        await _signalManager.WaitAsync<TriggerChangeTokenSignalEventArgs>(TriggerChangeTokenSignal);
        await _signalManager.WaitAsync<TriggerChangeTokenSignalEventArgs>(GraphChangeTokenSignal);

        Assert.False(_cache.TryGetValue($"http-workflow:{hash}", out _));
        Assert.False(_cache.TryGetValue($"IEnumerable`1:{hashedFilter}", out _));
        Assert.False(_cache.TryGetValue(parentVersionCacheKey, out _));
    }

    private void OnChangeTokenSignalTriggered(object? sender, TriggerChangeTokenSignalEventArgs args)
    {
        if (args.Key == _httpChangeToken)
        {
            _signalManager.Trigger(HttpChangeTokenSignal, args);
        }
        if (args.Key == _triggerChangeToken)
        {
            _signalManager.Trigger(TriggerChangeTokenSignal, args);
        }
        if (args.Key == _graphChangeToken)
        {
            _signalManager.Trigger(GraphChangeTokenSignal, args);
        }
    }
}