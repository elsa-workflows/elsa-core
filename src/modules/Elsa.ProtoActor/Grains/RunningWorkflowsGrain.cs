using Elsa.ProtoActor.Protos;
using Proto;
using Proto.Cluster;
using Proto.Persistence;
using Proto.Persistence.SnapshotStrategies;

namespace Elsa.ProtoActor.Grains;

/// <summary>
/// Represents a registry of workflow instances for a given workflow definition version.
/// </summary>
public class RunningWorkflowsGrain : RunningWorkflowsGrainBase
{
    private readonly Persistence _persistence;
    private IDictionary<string, WorkflowInstanceEntry> _lookupByInstanceId = new Dictionary<string, WorkflowInstanceEntry>();
    private IDictionary<string, WorkflowInstanceEntry> _lookupByCorrelationId = new Dictionary<string, WorkflowInstanceEntry>();

    /// <inheritdoc />
    public RunningWorkflowsGrain(IProvider provider, IContext context) : base(context)
    {
        _persistence = Persistence.WithEventSourcingAndSnapshotting(
            provider, 
            provider, 
            context.ClusterIdentity()!.Identity, 
            ApplyEvent, 
            ApplySnapshot, 
            new IntervalStrategy(50),
            GetState);
    }

    /// <inheritdoc />
    public override async Task Register(RegisterRunningWorkflowRequest request) => await _persistence.PersistEventAsync(request);

    /// <inheritdoc />
    public override async Task Unregister(UnregisterRunningWorkflowRequest request) => await _persistence.PersistEventAsync(request);

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
        var registrySnapshot = (WorkflowRegistrySnapshot)snapshot.State;
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

    private object GetState() => new WorkflowRegistrySnapshot(_lookupByInstanceId.Values);
    
    private void RegisterInternal(RegisterRunningWorkflowRequest request)
    {
        var entry = new WorkflowInstanceEntry(request.DefinitionId, request.Version, request.InstanceId, request.CorrelationId);
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

internal record WorkflowInstanceEntry(string DefinitionId, int Version, string InstanceId, string? CorrelationId);