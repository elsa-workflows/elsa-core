using Elsa.ProtoActor.Extensions;
using Elsa.ProtoActor.Models;
using Elsa.ProtoActor.ProtoBuf;
using Proto;
using Proto.Cluster;
using Proto.Persistence;
using Proto.Persistence.SnapshotStrategies;

namespace Elsa.ProtoActor.Grains;

/// <summary>
/// Represents a registry of workflow instances for a given workflow definition version.
/// </summary>
public class RunningWorkflows : RunningWorkflowsBase
{
    private const int EventsPerSnapshot = 1;
    private readonly Persistence _persistence;
    private IDictionary<string, RunningWorkflowInstanceEntry> _lookupByInstanceId = new Dictionary<string, RunningWorkflowInstanceEntry>();
    private IDictionary<string, RunningWorkflowInstanceEntry> _lookupByCorrelationId = new Dictionary<string, RunningWorkflowInstanceEntry>();

    /// <inheritdoc />
    public RunningWorkflows(IProvider provider, IContext context) : base(context)
    {
        _persistence = Persistence.WithEventSourcingAndSnapshotting(
            provider, 
            provider, 
            context.ClusterIdentity()!.Identity, 
            ApplyEvent, 
            ApplySnapshot, 
            new IntervalStrategy(EventsPerSnapshot),
            GetState);
    }

    /// <inheritdoc />
    public override async Task OnStarted()
    {
        await _persistence.RecoverStateAsync();
    }

    /// <inheritdoc />
    public override async Task Register(RegisterRunningWorkflowRequest request) => await _persistence.PersistRollingEventAsync(request, EventsPerSnapshot);

    /// <inheritdoc />
    public override async Task Unregister(UnregisterRunningWorkflowRequest request) => await _persistence.PersistRollingEventAsync(request, EventsPerSnapshot);

    /// <inheritdoc />
    public override Task<CountRunningWorkflowsResponse> Count(CountRunningWorkflowsRequest request)
    {
        var query = _lookupByInstanceId.Values.AsQueryable();

        if (!string.IsNullOrEmpty(request.DefinitionId)) query = query.Where(x => x.DefinitionId == request.DefinitionId);
        if (request.Version > 0) query = query.Where(x => x.Version == request.Version);
        if (!string.IsNullOrEmpty(request.CorrelationId)) query = query.Where(x => x.CorrelationId == request.CorrelationId);

        var count = query.Count();
        
        var response = new CountRunningWorkflowsResponse
        {
            Count = count
        };
        
        return Task.FromResult(response);
    }
    
    private void ApplySnapshot(Snapshot snapshot)
    {
        var registrySnapshot = (RunningWorkflowsSnapshot)snapshot.State;
        _lookupByCorrelationId = registrySnapshot.Entries.Where(x => !string.IsNullOrEmpty(x.CorrelationId)).ToDictionary(x => x.CorrelationId!);
        _lookupByInstanceId = registrySnapshot.Entries.ToDictionary(x => x.InstanceId);
    }

    private void ApplyEvent(Event @event)
    {
        switch (@event.Data)
        {
            case RegisterRunningWorkflowRequest registerWorkflowRequest:
                RegisterInternal(registerWorkflowRequest);
                break;
            case UnregisterRunningWorkflowRequest unregisterWorkflowRequest:
                UnregisterInternal(unregisterWorkflowRequest);
                break;
        }
    }

    private object GetState() => new RunningWorkflowsSnapshot(_lookupByInstanceId.Values.ToList());
    
    private void RegisterInternal(RegisterRunningWorkflowRequest request)
    {
        var entry = new RunningWorkflowInstanceEntry(request.DefinitionId, request.Version, request.InstanceId, request.CorrelationId);
        _lookupByInstanceId[request.InstanceId] = entry;

        if (!string.IsNullOrEmpty(request.CorrelationId))
            _lookupByCorrelationId[request.CorrelationId] = entry;
    }
    
    private void UnregisterInternal(UnregisterRunningWorkflowRequest request)
    {
        var correlationIds = _lookupByCorrelationId.Values.Where(x => x.InstanceId == request.InstanceId).Select(x => x.CorrelationId!).ToHashSet();

        foreach (var correlationId in correlationIds) 
            _lookupByCorrelationId.Remove(correlationId);
        
        _lookupByInstanceId.Remove(request.InstanceId);
    }
}