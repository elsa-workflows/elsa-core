using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Resolvers;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>
/// Finds native compensation scopes through Studio's known workflow activity edges. A marker in arbitrary JSON
/// data is only a reason to inspect the graph; it is never sufficient to classify the document as native.
/// </summary>
public sealed class NativeCompensationDraftGraphClassifier : INativeCompensationDraftGraphClassifier
{
    private static readonly HashSet<string> ReservedProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "inputs", "outputs", "metadata", "customProperties", "extensionData"
    };

    private readonly IActivityRegistry? _activityRegistry;
    private readonly IActivityPortService? _activityPortService;
    private readonly IReadOnlyList<IActivityResolver> _activityResolvers;
    private readonly IBackendApiClientProvider? _backendApiClientProvider;

    /// <summary>
    /// Creates the conservative legacy-constructor fallback. It recognizes direct native roots but cannot qualify
    /// arbitrary plugin graphs because it has no registered catalog, port providers, or activity resolvers.
    /// </summary>
    public NativeCompensationDraftGraphClassifier()
    {
        _activityResolvers = [];
    }

    public NativeCompensationDraftGraphClassifier(
        IActivityRegistry activityRegistry,
        IActivityPortService activityPortService,
        IEnumerable<IActivityResolver> activityResolvers,
        IBackendApiClientProvider backendApiClientProvider)
    {
        _activityRegistry = activityRegistry;
        _activityPortService = activityPortService;
        _activityResolvers = activityResolvers.OrderByDescending(resolver => resolver.Priority).ToArray();
        _backendApiClientProvider = backendApiClientProvider;
    }

    public async Task<NativeCompensationDraftGraphAnalysis> AnalyzeAsync(JsonObject? root, CancellationToken cancellationToken = default)
    {
        var snapshot = root?.DeepClone().AsObject();
        if (!NativeCompensationDraftCapability.ContainsPotentialScopeMarker(snapshot))
            return NativeCompensationDraftGraphAnalysis.Ordinary;

        if (_activityRegistry == null || _activityPortService == null || _backendApiClientProvider == null)
            return AnalyzeWithLegacyFallback(snapshot);

        var backendContextKey = _backendApiClientProvider.ContextKey;
        var state = new AnalysisState();

        try
        {
            await _activityRegistry.EnsureLoadedAsync(cancellationToken);
            if (!IsCurrentBackendContext(backendContextKey))
                return NativeCompensationDraftGraphAnalysis.Indeterminate;

            if (snapshot != null)
                await VisitActivityAsync(snapshot, state, new HashSet<JsonObject>(), backendContextKey, cancellationToken);

            if (!IsCurrentBackendContext(backendContextKey))
                return NativeCompensationDraftGraphAnalysis.Indeterminate;

            return new(
                !state.IsIndeterminate,
                state.ContainsCompensationScope,
                state.ContainsUnsupportedScope,
                HasPotentialScopeMarker: true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return NativeCompensationDraftGraphAnalysis.Indeterminate;
        }
    }

    private async Task VisitActivityAsync(
        JsonObject activity,
        AnalysisState state,
        HashSet<JsonObject> visited,
        string backendContextKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!visited.Add(activity) || !ContainsPotentialMarkerOutsideReservedData(activity))
            return;

        var typeName = GetString(activity, "type");
        if (NativeCompensationDraftCapability.IsCompensationScopeActivity(typeName))
        {
            await VisitCompensationScopeAsync(activity, state, visited, backendContextKey, cancellationToken);
            return;
        }

        if (string.Equals(typeName, "Elsa.NotFoundActivity", StringComparison.Ordinal))
        {
            await VisitNotFoundActivityAsync(activity, state, visited, backendContextKey, cancellationToken);
            return;
        }

        // ActivityVisitorExtensions wraps the workflow root in this synthetic activity for Studio's graph view.
        // It is not a catalog descriptor; its root is nevertheless an authoritative owned activity edge.
        if (string.Equals(typeName, "Elsa.Workflow", StringComparison.Ordinal))
        {
            if (activity["root"] is JsonObject workflowRoot)
                await VisitOwnedChildAsync(workflowRoot, state, visited, backendContextKey, cancellationToken);
            return;
        }

        if (!TryGetTypeVersion(activity, out typeName, out var version))
        {
            state.IsIndeterminate = true;
            return;
        }

        var descriptor = _activityRegistry!.Find(typeName, version);
        if (descriptor == null || descriptor.Version != version || !string.Equals(descriptor.TypeName, typeName, StringComparison.Ordinal))
        {
            state.IsIndeterminate = true;
            return;
        }

        if (!IsCurrentBackendContext(backendContextKey))
        {
            state.IsIndeterminate = true;
            return;
        }

        var context = new PortProviderContext(descriptor, activity);
        var provider = _activityPortService!.GetProvider(context);
        var embeddedPorts = _activityPortService.GetPorts(context)
            .Where(port => port.Type == PortType.Embedded)
            .ToArray();
        var embeddedPortNames = embeddedPorts.Select(port => port.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var port in embeddedPorts)
        {
            var child = provider.ResolvePort(port.Name, context);
            if (child != null)
                await VisitOwnedChildAsync(child, state, visited, backendContextKey, cancellationToken);
        }

        var resolverOwnedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (TryGetSpecificResolver(activity, out var resolver) && resolver is not DefaultActivityResolver)
        {
            var embeddedActivities = await resolver.GetActivitiesAsync(activity, cancellationToken);
            if (!IsCurrentBackendContext(backendContextKey))
            {
                state.IsIndeterminate = true;
                return;
            }

            foreach (var embeddedActivity in embeddedActivities)
            {
                resolverOwnedProperties.Add(embeddedActivity.PropertyName);
                await VisitOwnedChildAsync(embeddedActivity.Activity, state, visited, backendContextKey, cancellationToken);
            }
        }

        var excludedProperties = new HashSet<string>(ReservedProperties, StringComparer.OrdinalIgnoreCase);
        excludedProperties.UnionWith(descriptor.Inputs.Select(input => input.Name));
        excludedProperties.UnionWith(descriptor.Outputs.Select(output => output.Name));
        excludedProperties.UnionWith(embeddedPortNames);
        excludedProperties.UnionWith(resolverOwnedProperties);

        var typeKey = typeName;
        switch (typeKey)
        {
            case "Elsa.Sequence":
            case "Elsa.Flowchart":
                excludedProperties.Add("activities");
                await VisitActivityCollectionAsync(activity["activities"], state, visited, backendContextKey, cancellationToken);
                break;
            case "Elsa.StateMachine":
                excludedProperties.Add("states");
                excludedProperties.Add("transitions");
                await VisitStateMachineChildrenAsync(activity, state, visited, backendContextKey, cancellationToken);
                break;
        }

        foreach (var property in activity)
        {
            if (excludedProperties.Contains(property.Key))
                continue;

            await VisitDefaultResolverCandidatesAsync(property.Value, state, visited, backendContextKey, cancellationToken);
        }
    }

    private async Task VisitCompensationScopeAsync(
        JsonObject scope,
        AnalysisState state,
        HashSet<JsonObject> visited,
        string backendContextKey,
        CancellationToken cancellationToken)
    {
        state.ContainsCompensationScope = true;
        if (!NativeCompensationDraftCapability.HasSupportedDraftShape(scope) || string.IsNullOrWhiteSpace(GetString(scope, "id")))
            state.ContainsUnsupportedScope = true;

        if (scope["steps"] is not JsonArray steps)
            return;

        foreach (var step in steps.OfType<JsonObject>())
        {
            foreach (var role in new[] { "forward", "handler" })
            {
                if (step[role] is not JsonObject child)
                    continue;

                if (NativeCompensationDraftCapability.IsCompensationScopeActivity(GetString(child, "type")) ||
                    string.Equals(GetString(child, "type"), "Elsa.NotFoundActivity", StringComparison.Ordinal))
                {
                    await VisitActivityAsync(child, state, visited, backendContextKey, cancellationToken);
                    state.ContainsUnsupportedScope = true;
                    continue;
                }

                if (!TryGetTypeVersion(child, out var childType, out var childVersion) ||
                    string.IsNullOrWhiteSpace(GetString(child, "id")))
                {
                    state.ContainsUnsupportedScope = true;
                    if (ContainsPotentialMarkerOutsideReservedData(child))
                        state.IsIndeterminate = true;
                    continue;
                }

                var childDescriptor = _activityRegistry!.Find(childType, childVersion);
                if (childDescriptor == null || childDescriptor.Version != childVersion ||
                    !string.Equals(childDescriptor.TypeName, childType, StringComparison.Ordinal) ||
                    childType == "Elsa.NotFoundActivity" ||
                    childDescriptor.CustomProperties.TryGetValue("RootType", out var rootType) &&
                    rootType?.ToString() == "WorkflowDefinitionActivity")
                {
                    state.ContainsUnsupportedScope = true;
                    continue;
                }

                await VisitActivityAsync(child, state, visited, backendContextKey, cancellationToken);
            }
        }
    }

    private async Task VisitNotFoundActivityAsync(
        JsonObject activity,
        AnalysisState state,
        HashSet<JsonObject> visited,
        string backendContextKey,
        CancellationToken cancellationToken)
    {
        if (NativeCompensationDraftCapability.IsCompensationScopeActivity(GetString(activity, "missingTypeName")))
        {
            state.ContainsCompensationScope = true;
            state.ContainsUnsupportedScope = true;
            return;
        }

        if (NativeCompensationDraftCapability.TryParseOriginalActivity(activity, out var original) && original is JsonObject originalActivity)
        {
            await VisitActivityAsync(originalActivity, state, visited, backendContextKey, cancellationToken);
            if (NativeCompensationDraftCapability.IsCompensationScopeActivity(GetString(originalActivity, "type")) ||
                NativeCompensationDraftCapability.IsOpaqueCompensationScope(originalActivity))
                state.ContainsUnsupportedScope = true;
        }
    }

    private async Task VisitActivityCollectionAsync(
        JsonNode? collection,
        AnalysisState state,
        HashSet<JsonObject> visited,
        string backendContextKey,
        CancellationToken cancellationToken)
    {
        var children = collection switch
        {
            JsonArray array => array.OfType<JsonObject>(),
            JsonObject obj => obj.Select(property => property.Value).OfType<JsonObject>(),
            _ => []
        };

        foreach (var child in children)
            await VisitOwnedChildAsync(child, state, visited, backendContextKey, cancellationToken);
    }

    private async Task VisitStateMachineChildrenAsync(
        JsonObject activity,
        AnalysisState state,
        HashSet<JsonObject> visited,
        string backendContextKey,
        CancellationToken cancellationToken)
    {
        foreach (var stateNode in (activity["states"] as JsonArray ?? []).OfType<JsonObject>())
        {
            foreach (var slot in new[] { "entry", "exit" })
                if (stateNode[slot] is JsonObject child)
                    await VisitOwnedChildAsync(child, state, visited, backendContextKey, cancellationToken);
        }

        foreach (var transition in (activity["transitions"] as JsonArray ?? []).OfType<JsonObject>())
        {
            foreach (var slot in new[] { "trigger", "action" })
                if (transition[slot] is JsonObject child)
                    await VisitOwnedChildAsync(child, state, visited, backendContextKey, cancellationToken);
        }
    }

    private async Task VisitDefaultResolverCandidatesAsync(
        JsonNode? value,
        AnalysisState state,
        HashSet<JsonObject> visited,
        string backendContextKey,
        CancellationToken cancellationToken)
    {
        if (value is JsonObject child && IsActivity(child))
            await VisitOwnedChildAsync(child, state, visited, backendContextKey, cancellationToken);

        if (value is JsonArray array)
            foreach (var item in array.OfType<JsonObject>())
                if (IsActivity(item))
                    await VisitOwnedChildAsync(item, state, visited, backendContextKey, cancellationToken);
    }

    private async Task VisitOwnedChildAsync(
        JsonObject child,
        AnalysisState state,
        HashSet<JsonObject> visited,
        string backendContextKey,
        CancellationToken cancellationToken)
    {
        if (IsDirectNativeMarker(child))
        {
            await VisitActivityAsync(child, state, visited, backendContextKey, cancellationToken);
            return;
        }

        if (IsActivity(child))
        {
            await VisitActivityAsync(child, state, visited, backendContextKey, cancellationToken);
            return;
        }

        if (ContainsPotentialMarkerOutsideReservedData(child))
            state.IsIndeterminate = true;
    }

    private bool TryGetSpecificResolver(JsonObject activity, out IActivityResolver resolver)
    {
        resolver = _activityResolvers.FirstOrDefault(candidate => candidate.GetSupportsActivity(activity))!;
        return resolver != null;
    }

    private bool IsCurrentBackendContext(string backendContextKey) =>
        string.Equals(_backendApiClientProvider!.ContextKey, backendContextKey, StringComparison.Ordinal);

    private static bool IsActivity(JsonObject activity) =>
        activity.IsActivity() && TryGetTypeVersion(activity, out _, out _);

    private static bool IsDirectNativeMarker(JsonObject activity) =>
        NativeCompensationDraftCapability.IsCompensationScopeActivity(GetString(activity, "type")) ||
        string.Equals(GetString(activity, "type"), "Elsa.NotFoundActivity", StringComparison.Ordinal);

    private static bool TryGetTypeVersion(JsonObject activity, out string typeName, out int version)
    {
        typeName = GetString(activity, "type") ?? string.Empty;
        version = GetInt32(activity, "version") ?? 0;
        return !string.IsNullOrWhiteSpace(typeName) && version > 0;
    }

    private static bool ContainsPotentialMarkerOutsideReservedData(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            var typeName = GetString(obj, "type");
            if (NativeCompensationDraftCapability.IsCompensationScopeActivity(typeName) ||
                string.Equals(typeName, "Elsa.NotFoundActivity", StringComparison.Ordinal))
                return NativeCompensationDraftCapability.ContainsPotentialScopeMarker(obj);

            return obj.Any(property => !ReservedProperties.Contains(property.Key) &&
                                       ContainsPotentialMarkerOutsideReservedData(property.Value));
        }

        return node is JsonArray array && array.Any(ContainsPotentialMarkerOutsideReservedData);
    }

    private static NativeCompensationDraftGraphAnalysis AnalyzeWithLegacyFallback(JsonObject? root)
    {
        if (root == null)
            return NativeCompensationDraftGraphAnalysis.Ordinary;

        return AnalyzeLegacyActivity(root, new HashSet<JsonObject>());
    }

    private static NativeCompensationDraftGraphAnalysis AnalyzeLegacyActivity(JsonObject activity, HashSet<JsonObject> visited)
    {
        if (!visited.Add(activity))
            return NativeCompensationDraftGraphAnalysis.Ordinary;

        var hasPotentialMarker = NativeCompensationDraftCapability.ContainsPotentialScopeMarker(activity);
        if (!hasPotentialMarker)
            return NativeCompensationDraftGraphAnalysis.Ordinary;

        if (!ContainsPotentialMarkerOutsideReservedData(activity))
            return new(true, false, false, true);

        var typeName = GetString(activity, "type");
        if (NativeCompensationDraftCapability.IsCompensationScopeActivity(typeName))
        {
            var isSupported = NativeCompensationDraftCapability.HasSupportedDraftShape(activity) &&
                              !string.IsNullOrWhiteSpace(GetString(activity, "id"));
            var containsUnverifiedPin = activity["steps"] is JsonArray steps && steps.OfType<JsonObject>().Any(step =>
                step["forward"] is JsonObject || step["handler"] is JsonObject);
            var containsAmbiguousData = HasPotentialMarkerOutsideProperties(activity, ["steps"]) ||
                                        activity["steps"] is JsonArray stepRows && stepRows.OfType<JsonObject>().Any(step =>
                                            HasPotentialMarkerOutsideProperties(step, ["forward", "handler", "stepId", "mappings"]));
            return new(!containsAmbiguousData, true, !isSupported || containsUnverifiedPin || containsAmbiguousData, true);
        }

        if (string.Equals(typeName, "Elsa.Workflow", StringComparison.Ordinal))
        {
            var unownedMarker = HasPotentialMarkerOutsideProperties(activity, ["root"]);
            var workflowRootAnalysis = activity["root"] is JsonObject workflowRoot
                ? AnalyzeLegacyActivity(workflowRoot, visited)
                : NativeCompensationDraftGraphAnalysis.Ordinary;
            if (unownedMarker)
                return NativeCompensationDraftGraphAnalysis.Indeterminate;

            return workflowRootAnalysis;
        }

        if (string.Equals(typeName, "Elsa.NotFoundActivity", StringComparison.Ordinal) &&
            NativeCompensationDraftCapability.IsOpaqueCompensationScope(activity))
            return new(true, true, true, true);

        if (string.Equals(typeName, "Elsa.NotFoundActivity", StringComparison.Ordinal) &&
            NativeCompensationDraftCapability.TryParseOriginalActivity(activity, out var original) && original is JsonObject originalActivity)
        {
            var originalAnalysis = AnalyzeLegacyActivity(originalActivity, visited);
            return originalAnalysis.RequiresNativeProtection
                ? new(originalAnalysis.IsComplete, originalAnalysis.ContainsCompensationScope, true, true)
                : new(true, false, false, true);
        }

        if (typeName is "Elsa.Sequence" or "Elsa.Flowchart")
        {
            if (HasProtectedLegacyActivityInCollection(activity["activities"], visited) ||
                HasPotentialMarkerOutsideProperties(activity, ["activities", "connections"]))
                return NativeCompensationDraftGraphAnalysis.Indeterminate;

            return new(true, false, false, true);
        }

        if (string.Equals(typeName, "Elsa.If", StringComparison.Ordinal))
        {
            if (HasProtectedLegacyActivity(activity["then"], visited) || HasProtectedLegacyActivity(activity["else"], visited) ||
                HasPotentialMarkerOutsideProperties(activity, ["then", "else", "condition"]))
                return NativeCompensationDraftGraphAnalysis.Indeterminate;

            return new(true, false, false, true);
        }

        if (string.Equals(typeName, "Elsa.StateMachine", StringComparison.Ordinal))
        {
            if (HasProtectedLegacyStateMachineActivity(activity, visited) ||
                HasPotentialMarkerOutsideProperties(activity, ["states", "transitions", "initialState"]))
                return NativeCompensationDraftGraphAnalysis.Indeterminate;

            return new(true, false, false, true);
        }

        if (string.Equals(typeName, "Elsa.SetVariable", StringComparison.Ordinal))
        {
            if (!IsActivity(activity) || GetInt32(activity, "version") != 1 ||
                HasPotentialMarkerOutsideProperties(activity, ["variable", "value"]))
                return NativeCompensationDraftGraphAnalysis.Indeterminate;

            return new(true, false, false, true);
        }

        // The legacy overloads do not have the descriptor catalog, dynamic port providers, or custom resolver
        // registrations. Unknown marker-bearing fields therefore remain ambiguous and fail closed.
        return NativeCompensationDraftGraphAnalysis.Indeterminate;
    }

    private static bool HasPotentialMarkerOutsideProperties(JsonObject activity, IReadOnlyCollection<string> excludedProperties)
    {
        var excluded = excludedProperties.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return activity.Any(property => !ReservedProperties.Contains(property.Key) && !excluded.Contains(property.Key) &&
                                        ContainsPotentialMarkerOutsideReservedData(property.Value));
    }

    private static bool HasProtectedLegacyActivity(JsonNode? node, HashSet<JsonObject> visited)
    {
        if (node is JsonObject activity)
            return AnalyzeLegacyActivity(activity, visited).RequiresNativeProtection;

        return ContainsPotentialMarkerOutsideReservedData(node);
    }

    private static bool HasProtectedLegacyActivityInCollection(JsonNode? node, HashSet<JsonObject> visited)
    {
        IEnumerable<JsonNode?> children = node switch
        {
            JsonArray array => array,
            JsonObject obj => obj.Select(property => property.Value),
            _ => []
        };

        foreach (var child in children)
            if (HasProtectedLegacyActivity(child, visited))
                return true;

        return node is not (JsonArray or JsonObject) && ContainsPotentialMarkerOutsideReservedData(node);
    }

    private static bool HasProtectedLegacyStateMachineActivity(JsonObject activity, HashSet<JsonObject> visited)
    {
        foreach (var state in (activity["states"] as JsonArray ?? []).OfType<JsonObject>())
        {
            if (HasProtectedLegacyActivity(state["entry"], visited) || HasProtectedLegacyActivity(state["exit"], visited) ||
                HasPotentialMarkerOutsideProperties(state, ["entry", "exit", "name"]))
                return true;
        }

        foreach (var transition in (activity["transitions"] as JsonArray ?? []).OfType<JsonObject>())
        {
            if (HasProtectedLegacyActivity(transition["trigger"], visited) || HasProtectedLegacyActivity(transition["action"], visited) ||
                HasPotentialMarkerOutsideProperties(transition, ["trigger", "action", "source", "target"]))
                return true;
        }

        return activity["states"] is not null && activity["states"] is not JsonArray && ContainsPotentialMarkerOutsideReservedData(activity["states"]) ||
               activity["transitions"] is not null && activity["transitions"] is not JsonArray && ContainsPotentialMarkerOutsideReservedData(activity["transitions"]);
    }

    private static string? GetString(JsonObject? obj, string key) =>
        obj?[key] is JsonValue value && value.TryGetValue<string>(out var result) ? result : null;

    private static int? GetInt32(JsonObject obj, string key) =>
        obj[key] is JsonValue value && value.TryGetValue<int>(out var result) ? result : null;

    private sealed class AnalysisState
    {
        public bool IsIndeterminate { get; set; }
        public bool ContainsCompensationScope { get; set; }
        public bool ContainsUnsupportedScope { get; set; }
    }
}
