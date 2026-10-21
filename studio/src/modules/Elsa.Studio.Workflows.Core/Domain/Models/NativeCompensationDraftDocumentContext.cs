using System.Runtime.CompilerServices;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

namespace Elsa.Studio.Workflows.Domain.Models;

/// <summary>
/// Keeps the backend origin of an in-memory compensation draft separate from serialized workflow data.
/// Bindings are immutable for the lifetime of a model so adopting a late response cannot restamp it.
/// </summary>
public static class NativeCompensationDraftDocumentContext
{
    private sealed record Origin(string BackendContextKey);
    private static readonly ConditionalWeakTable<WorkflowDefinition, Origin> Origins = new();

    public static string? GetBackendContextKey(WorkflowDefinition? definition) =>
        definition != null && Origins.TryGetValue(definition, out var origin) ? origin.BackendContextKey : null;

    public static bool TryBind(WorkflowDefinition definition, string backendContextKey)
    {
        var origin = Origins.GetValue(definition, _ => new(backendContextKey));
        return string.Equals(origin.BackendContextKey, backendContextKey, StringComparison.Ordinal);
    }

    public static bool Copy(WorkflowDefinition source, WorkflowDefinition destination)
    {
        var origin = GetBackendContextKey(source);
        return origin != null && TryBind(destination, origin);
    }
}
