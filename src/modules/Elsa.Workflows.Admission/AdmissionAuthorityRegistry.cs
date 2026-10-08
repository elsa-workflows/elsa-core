using System.Collections.Concurrent;
using Elsa.Expressions.Models;
using Microsoft.Extensions.DependencyInjection;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Admission;

// One registry per isolated execution host. No capability is public, serialized, ambient,
// stored in context properties or accepted by a public pipeline API.
internal sealed class AdmissionAuthorityRegistry
{
    private readonly ConcurrentDictionary<WorkflowExecutionContext, Invocation> _invocations = new(ReferenceEqualityComparer.Instance);
    private readonly ConcurrentDictionary<string, byte> _owners = new(StringComparer.Ordinal);

    public IDisposable AcquireOwner(string instanceId)
    {
        if (!_owners.TryAdd(instanceId, 0))
        {
            throw new InvalidOperationException("An admission invocation already owns this workflow instance.");
        }
        return new Owner(_owners, instanceId);
    }

    public bool HasOwner(string instanceId) => _owners.ContainsKey(instanceId);

    public Invocation Capture(WorkflowExecutionContext context, IActivitySerializer serializer, IWorkflowStateExtractor extractor, IWorkflowStateSerializer stateSerializer)
    {
        var invocation = new Invocation(context, serializer, extractor, stateSerializer);
        if (!_invocations.TryAdd(context, invocation))
        {
            throw new InvalidOperationException("This prepared context is already protected.");
        }
        return invocation;
    }

    public void Bind(WorkflowExecutionContext context, Invocation invocation, AdmissionRecord record, AdmissionSubscriptionConfiguration configuration)
    {
        if (record.State != AdmissionState.StartAuthorized || !record.AuthorityOutstanding || record.WorkflowInstanceId != context.Id ||
            record.ConfigurationFingerprint != configuration.ConfigurationFingerprint || context.Workflow.Identity.Id != configuration.DefinitionVersionId ||
            context.Workflow.Identity.DefinitionId != configuration.DefinitionId || context.Workflow.Identity.Version != configuration.DefinitionVersion ||
            record.AttemptId == null || !_owners.ContainsKey(context.Id))
        {
            throw new InvalidOperationException("The admission invocation does not match its committed authority.");
        }
        if (!_invocations.TryGetValue(context, out var captured) || !ReferenceEquals(invocation, captured))
        {
            throw new InvalidOperationException("This prepared context is not protected by the invocation registry.");
        }
        invocation.BindAuthority(record);
    }

    public Invocation? Consume(WorkflowExecutionContext context, WorkflowExecutionEntryPoint entryPoint)
    {
        if (!_invocations.TryGetValue(context, out var invocation))
        {
            return null;
        }
        if (entryPoint != WorkflowExecutionEntryPoint.Runner)
        {
            throw new InvalidOperationException("Owned workflows cannot execute through a public pipeline.");
        }
        invocation.DemandBound();
        invocation.Validate();
        if (Interlocked.CompareExchange(ref invocation.Consumed, 1, 0) != 0)
        {
            throw new InvalidOperationException("Admission execution authority is one-shot.");
        }
        return invocation;
    }

    public void Retire(WorkflowExecutionContext context) => _invocations.TryRemove(context, out _);

    private sealed class Owner(ConcurrentDictionary<string, byte> owners, string instanceId) : IDisposable
    {
        private int _disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                owners.TryRemove(instanceId, out _);
            }
        }
    }

    internal sealed class Invocation : IWorkflowExecutionAuthorization
    {
        private readonly WorkflowExecutionContext _context;
        private readonly WorkflowGraph _graph;
        private readonly AdmissionExecutionComposition _composition;
        private readonly IActivitySerializer _serializer;
        private readonly IWorkflowStateExtractor _extractor;
        private readonly IWorkflowStateSerializer _stateSerializer;
        private readonly ActivityNode[] _nodes;
        private readonly KeyValuePair<string, ActivityNode>[] _nodeIds;
        private readonly KeyValuePair<string, ActivityNode>[] _nodeHashes;
        private readonly KeyValuePair<IActivity, ActivityNode>[] _nodeActivities;
        private readonly ActivityNode[][] _parents;
        private readonly ActivityNode[][] _children;
        private readonly ActivityCompletionCallbackEntry[] _callbacks;
        private readonly object?[] _callbackDelegates;
        private readonly object[] _callbackOwners;
        private readonly ActivityNode[] _callbackChildren;
        private readonly string _identity;
        private readonly string _prepared;
        private readonly object? _executeDelegate;
        private readonly object? _root;
        private readonly object? _resumedBookmark;
        private readonly WorkflowSubStatus _subStatus;
        private readonly ActivityWorkItem[] _workItems;
        private readonly object?[] _workItemActivities;
        private readonly object?[] _workItemOwners;
        private readonly object?[] _existingContexts;
        private string? _authorityTuple;
        internal string AdmissionId { get; private set; } = null!;
        internal string InstanceId => _identity;
        public int Consumed;

        public Invocation(WorkflowExecutionContext context, IActivitySerializer serializer, IWorkflowStateExtractor extractor, IWorkflowStateSerializer stateSerializer)
        {
            _context = context;
            _graph = context.WorkflowGraph;
            _composition = context.GetRequiredService<AdmissionExecutionComposition>();
            _composition.Validate(context.ServiceProvider);
            _serializer = serializer;
            _extractor = extractor;
            _stateSerializer = stateSerializer;
            _nodes = _graph.Nodes.ToArray();
            _nodeIds = _graph.NodeIdLookup.ToArray();
            _nodeHashes = _graph.NodeHashLookup.ToArray();
            _nodeActivities = _graph.NodeActivityLookup.ToArray();
            _parents = _nodes.Select(x => x.Parents.ToArray()).ToArray();
            _children = _nodes.Select(x => x.Children.ToArray()).ToArray();
            _callbacks = context.CompletionCallbacks.ToArray();
            _callbackDelegates = _callbacks.Select(x => (object?)x.CompletionCallback).ToArray();
            _callbackOwners = _callbacks.Select(x => (object)x.Owner).ToArray();
            _callbackChildren = _callbacks.Select(x => x.Child).ToArray();
            _identity = context.Id;
            _executeDelegate = context.ExecuteDelegate;
            _root = context.Workflow.Root;
            _resumedBookmark = context.ResumedBookmarkContext;
            _subStatus = context.SubStatus;
            _workItems = context.Scheduler.List().ToArray();
            _workItemActivities = _workItems.Select(x => (object?)x.Activity).ToArray();
            _workItemOwners = _workItems.Select(x => (object?)x.Owner).ToArray();
            _existingContexts = _workItems.Select(x => (object?)x.ExistingActivityExecutionContext).ToArray();
            _prepared = Fingerprint();
        }

        public void BindAuthority(AdmissionRecord record)
        {
            if (_authorityTuple != null)
            {
                throw new InvalidOperationException("The prepared invocation is already bound.");
            }
            Validate();
            // Immutable copied values, never a mutable record or context stamp. Binding occurs
            // only after a definitive first commit, without recapturing the prepared fingerprint.
            _authorityTuple = AdmissionHash.Compute($"{record.Id}:{record.Revision}:{record.AttemptId}:{record.ConfigurationFingerprint}");
            AdmissionId = record.Id;
        }

        public void DemandBound()
        {
            if (_authorityTuple == null)
            {
                throw new InvalidOperationException("Prepared admission work has no committed execution authority.");
            }
        }

        public ValueTask RevalidateAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_authorityTuple == null || Volatile.Read(ref Consumed) != 1)
            {
                throw new InvalidOperationException("Admission authority was not consumed.");
            }
            Validate();
            return ValueTask.CompletedTask;
        }

        public void Validate()
        {
            _composition.Validate(_context.ServiceProvider);
            var current = _context.Scheduler.List().ToArray();
            var expectedStatus = Volatile.Read(ref Consumed) == 1 && _subStatus == WorkflowSubStatus.Pending
                ? WorkflowSubStatus.Executing : _subStatus;
            if (_context.Id != _identity || !ReferenceEquals(_graph, _context.WorkflowGraph) ||
                !ReferenceEquals(_root, _context.Workflow.Root) || !ReferenceEquals(_resumedBookmark, _context.ResumedBookmarkContext) ||
                !ReferenceEquals(_executeDelegate, _context.ExecuteDelegate) || current.Length != _workItems.Length || _context.SubStatus != expectedStatus)
            {
                throw new InvalidOperationException("The prepared admission invocation changed.");
            }
            for (var i = 0; i < current.Length; i++)
            {
                if (!ReferenceEquals(current[i], _workItems[i]) || !ReferenceEquals(current[i].Activity, _workItemActivities[i]) ||
                    !ReferenceEquals(current[i].Owner, _workItemOwners[i]) || !ReferenceEquals(current[i].ExistingActivityExecutionContext, _existingContexts[i]))
                {
                    throw new InvalidOperationException("The prepared admission scheduling plan changed.");
                }
            }
            if (!_nodes.SequenceEqual(_graph.Nodes, ReferenceEqualityComparer.Instance) ||
                !_nodeIds.SequenceEqual(_graph.NodeIdLookup) || !_nodeHashes.SequenceEqual(_graph.NodeHashLookup) ||
                !_nodeActivities.SequenceEqual(_graph.NodeActivityLookup))
            {
                throw new InvalidOperationException("The prepared admission graph topology changed.");
            }
            for (var i = 0; i < _nodes.Length; i++)
            {
                if (!_parents[i].SequenceEqual(_nodes[i].Parents, ReferenceEqualityComparer.Instance) ||
                    !_children[i].SequenceEqual(_nodes[i].Children, ReferenceEqualityComparer.Instance))
                {
                    throw new InvalidOperationException("The prepared admission graph relationships changed.");
                }
            }
            var callbacks = _context.CompletionCallbacks.ToArray();
            if (!Enumerable.SequenceEqual(_callbacks, callbacks, ReferenceEqualityComparer.Instance))
            {
                throw new InvalidOperationException("The prepared admission completion callbacks changed.");
            }
            for (var i = 0; i < callbacks.Length; i++)
            {
                if (!ReferenceEquals(callbacks[i].CompletionCallback, _callbackDelegates[i]) ||
                    !ReferenceEquals(callbacks[i].Owner, _callbackOwners[i]) || !ReferenceEquals(callbacks[i].Child, _callbackChildren[i]))
                {
                    throw new InvalidOperationException("The prepared admission completion callback identities changed.");
                }
            }
            if (_prepared != Fingerprint())
            {
                throw new InvalidOperationException("The prepared admission fingerprint changed.");
            }
        }

        private string Fingerprint()
        {
            var plan = _context.Scheduler.List().Select(item => (object)new Dictionary<string, object>
            {
                ["activity"] = item.Activity.Id,
                ["owner"] = item.Owner?.Id!,
                ["existing"] = item.ExistingActivityExecutionContext?.Id!,
                ["input"] = item.Input,
                ["tag"] = item.Tag!,
                ["schedulingActivity"] = item.SchedulingActivityExecutionId!,
                ["schedulingWorkflow"] = item.SchedulingWorkflowInstanceId!,
                ["depth"] = item.SchedulingCallStackDepth,
                ["variables"] = item.Variables?.Select(variable => (object)new Dictionary<string, object>
                {
                    ["id"] = variable.Id,
                    ["name"] = variable.Name,
                    ["value"] = variable.Value!,
                    ["storage"] = variable.StorageDriverType?.AssemblyQualifiedName!
                }).ToList()!
            }).ToList();
            var state = _extractor.Extract(_context);
            // The built-in runner alone changes these fields between its two guard boundaries.
            // Every other restored state, memory, bookmark and scheduling field remains bound.
            state.Status = WorkflowStatus.Running;
            state.SubStatus = WorkflowSubStatus.Pending;
            state.UpdatedAt = default;
            return AdmissionValueFingerprint.Compute(new Dictionary<string, object>
            {
                ["id"] = _context.Id,
                ["graph"] = _serializer.Serialize(_context.Workflow),
                ["state"] = _stateSerializer.Serialize(state),
                ["input"] = _context.Input,
                ["properties"] = _context.Properties,
                ["output"] = _context.Output,
                ["rootMemory"] = Memory(_context.MemoryRegister),
                ["activityMemory"] = _context.ActivityExecutionContexts.Select(x => (object)new Dictionary<string, object>
                {
                    ["id"] = x.Id,
                    ["memory"] = Memory(x.ExpressionExecutionContext.Memory)
                }).ToList(),
                ["resumedBookmark"] = _context.ResumedBookmarkContext == null ? null! : _stateSerializer.Serialize(_context.ResumedBookmarkContext.Bookmark),
                ["correlation"] = _context.CorrelationId!,
                ["parent"] = _context.ParentWorkflowInstanceId!,
                ["trigger"] = _context.TriggerActivityId!,
                ["plan"] = plan
            });
        }

        private static Dictionary<string, object> Memory(MemoryRegister register) => register.Blocks.ToDictionary(x => x.Key, x => (object)new Dictionary<string, object>
        {
            ["value"] = x.Value.Value!,
            ["metadata"] = x.Value.Metadata is VariableBlockMetadata metadata ? new Dictionary<string, object>
            {
                ["id"] = metadata.Variable.Id,
                ["name"] = metadata.Variable.Name,
                ["value"] = metadata.Variable.Value!,
                ["storage"] = metadata.StorageDriverType?.AssemblyQualifiedName!,
                ["variableStorage"] = metadata.Variable.StorageDriverType?.AssemblyQualifiedName!,
                ["initialized"] = metadata.IsInitialized
            } : x.Value.Metadata!
        }, StringComparer.Ordinal);
    }
}
