using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Activities.Compensation;

/// <summary>
/// Tracks only the mutable state that can change the draft-only compensation refusal predicate.
/// </summary>
internal static class CompensationScopeExecutionState
{
    private static readonly SaturatingRevision DiscriminatorEpoch = new();
    private static readonly ConditionalWeakTable<WorkflowGraph, GraphCache> GraphCaches = new();
    private static readonly ConditionalWeakTable<Type, ActivityTypeCacheability> ActivityTypeCacheabilityByClrType = new();
    private static readonly MethodInfo? ActivityTypeInterfaceGetter = typeof(IActivity).GetProperty(nameof(IActivity.Type))?.GetMethod;
    private static readonly MethodInfo? ActivityTypeInterfaceSetter = typeof(IActivity).GetProperty(nameof(IActivity.Type))?.SetMethod;
    private static readonly MethodInfo? ActivityTypeGetter = typeof(Activity).GetProperty(nameof(Activity.Type))?.GetMethod;
    private static readonly MethodInfo? ActivityTypeSetter = typeof(Activity).GetProperty(nameof(Activity.Type))?.SetMethod;

    internal static void OnDiscriminatorChanged(string? previousType, string? currentType)
    {
        if (IsCompensationScopeType(previousType) != IsCompensationScopeType(currentType))
            DiscriminatorEpoch.Increment();
    }

    internal static void OnOriginalActivityJsonChanged() => DiscriminatorEpoch.Increment();

    internal static bool TryGetPass(WorkflowGraph graph, out CacheObservation observation)
    {
        observation = default;

        if (graph.Nodes is not CompensationScopeExecutionNodeCollection nodes ||
            !nodes.Revision.TryGetValue(out var nodeRevision) ||
            !DiscriminatorEpoch.TryGetValue(out var discriminatorEpoch))
            return false;

        var cache = GraphCaches.GetValue(graph, static _ => new GraphCache());
        var classification = cache.GetClassification(nodeRevision);
        if (classification is { IsCacheable: false })
        {
            observation = new CacheObservation(nodes, cache, nodeRevision, discriminatorEpoch, false, false, true);
            return false;
        }

        var needsClassification = classification == null;
        observation = new CacheObservation(nodes, cache, nodeRevision, discriminatorEpoch, needsClassification, true, true);

        return classification is { IsCacheable: true } && cache.HasPass(nodeRevision, discriminatorEpoch);
    }

    internal static bool IsActivityTypeCacheable(IActivity activity) =>
        ActivityTypeCacheabilityByClrType.GetValue(activity.GetType(), static type => new ActivityTypeCacheability(ClassifyActivityType(type))).IsCacheable;

    internal static void RecordCompletedScan(CacheObservation observation, bool activitiesAreCacheable)
    {
        if (!observation.IsEnabled)
            return;

        if (!observation.Nodes.Revision.TryGetValue(out var currentNodeRevision) || currentNodeRevision != observation.NodeRevision)
            return;

        if (observation.NeedsClassification)
            observation.Cache.SetClassification(observation.NodeRevision, activitiesAreCacheable);

        if (!activitiesAreCacheable ||
            !DiscriminatorEpoch.TryGetValue(out var currentDiscriminatorEpoch) ||
            currentDiscriminatorEpoch != observation.DiscriminatorEpoch ||
            !observation.Nodes.Revision.TryGetValue(out currentNodeRevision) ||
            currentNodeRevision != observation.NodeRevision)
            return;

        observation.Cache.SetPass(observation.NodeRevision, observation.DiscriminatorEpoch);
    }

    private static bool IsCompensationScopeType(string? type) =>
        string.Equals(type, CompensationScope.ActivityTypeName, StringComparison.Ordinal);

    private static bool ClassifyActivityType(Type runtimeType)
    {
        if (!typeof(Activity).IsAssignableFrom(runtimeType) ||
            ActivityTypeInterfaceGetter == null || ActivityTypeInterfaceSetter == null ||
            ActivityTypeGetter == null || ActivityTypeSetter == null)
            return false;

        try
        {
            var interfaceMap = runtimeType.GetInterfaceMap(typeof(IActivity));
            return HasExactInterfaceTarget(interfaceMap, ActivityTypeInterfaceGetter, ActivityTypeGetter) &&
                   HasExactInterfaceTarget(interfaceMap, ActivityTypeInterfaceSetter, ActivityTypeSetter);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or NotSupportedException)
        {
            // Unsupported reflection metadata must use the uncached full scan.
            return false;
        }
    }

    private static bool HasExactInterfaceTarget(InterfaceMapping interfaceMap, MethodInfo interfaceMethod, MethodInfo expectedTarget)
    {
        var index = Array.IndexOf(interfaceMap.InterfaceMethods, interfaceMethod);
        return index >= 0 && interfaceMap.TargetMethods[index].MethodHandle.Equals(expectedTarget.MethodHandle);
    }

    internal readonly struct CacheObservation(
        CompensationScopeExecutionNodeCollection nodes,
        GraphCache cache,
        long nodeRevision,
        long discriminatorEpoch,
        bool needsClassification,
        bool activitiesAreCacheable,
        bool isEnabled)
    {
        internal CompensationScopeExecutionNodeCollection Nodes { get; } = nodes;
        internal GraphCache Cache { get; } = cache;
        internal long NodeRevision { get; } = nodeRevision;
        internal long DiscriminatorEpoch { get; } = discriminatorEpoch;
        internal bool NeedsClassification { get; } = needsClassification;
        internal bool ActivitiesAreCacheable { get; } = activitiesAreCacheable;
        internal bool IsEnabled { get; } = isEnabled;
    }

    internal sealed class GraphCache
    {
        private TrackabilityClassification? _classification;
        private PassSnapshot? _pass;

        internal TrackabilityClassification? GetClassification(long nodeRevision)
        {
            var classification = Volatile.Read(ref _classification);
            return classification?.NodeRevision == nodeRevision ? classification : null;
        }

        internal bool HasPass(long nodeRevision, long discriminatorEpoch)
        {
            var pass = Volatile.Read(ref _pass);
            return pass?.NodeRevision == nodeRevision && pass.DiscriminatorEpoch == discriminatorEpoch;
        }

        internal void SetClassification(long nodeRevision, bool isCacheable) =>
            Volatile.Write(ref _classification, new TrackabilityClassification(nodeRevision, isCacheable));

        internal void SetPass(long nodeRevision, long discriminatorEpoch) =>
            Volatile.Write(ref _pass, new PassSnapshot(nodeRevision, discriminatorEpoch));
    }

    internal sealed record TrackabilityClassification(long NodeRevision, bool IsCacheable);
    private sealed record PassSnapshot(long NodeRevision, long DiscriminatorEpoch);
    private sealed record ActivityTypeCacheability(bool IsCacheable);
}

/// <summary>
/// Keeps WorkflowGraph.Nodes list-backed enumeration semantics while tracking collection mutations.
/// </summary>
internal sealed class CompensationScopeExecutionNodeCollection : Collection<ActivityNode>
{
    internal CompensationScopeExecutionNodeCollection(IEnumerable<ActivityNode> nodes) : base(nodes.ToList())
    {
    }

    internal SaturatingRevision Revision { get; } = new();

    protected override void InsertItem(int index, ActivityNode item)
    {
        base.InsertItem(index, item);
        Revision.Increment();
    }

    protected override void SetItem(int index, ActivityNode item)
    {
        base.SetItem(index, item);
        Revision.Increment();
    }

    protected override void RemoveItem(int index)
    {
        base.RemoveItem(index);
        Revision.Increment();
    }

    protected override void ClearItems()
    {
        base.ClearItems();
        Revision.Increment();
    }
}

internal sealed class SaturatingRevision
{
    private long _value;
    private int _disabled;

    internal bool TryGetValue(out long value)
    {
        if (Volatile.Read(ref _disabled) != 0)
        {
            value = default;
            return false;
        }

        value = Volatile.Read(ref _value);
        return Volatile.Read(ref _disabled) == 0;
    }

    internal void Increment()
    {
        while (Volatile.Read(ref _disabled) == 0)
        {
            var current = Volatile.Read(ref _value);
            if (current == long.MaxValue)
            {
                Interlocked.Exchange(ref _disabled, 1);
                return;
            }

            if (Interlocked.CompareExchange(ref _value, current + 1, current) == current)
                return;
        }
    }
}
