using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Bpmn.Designers;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Extensions;

namespace Elsa.Studio.Workflows.DiagramDesigners.Bpmn;

/// <summary>
/// Which BPMN element each Elsa activity of an imported process is bound to, read once from the root activity's
/// scopes: each scope's <c>workBindings</c> name the activity a <c>bindingRef</c> runs, and its process elements carry
/// the <c>bindingRef</c>. This is how a designer that knows only element ids shows activity state and reports selection.
/// </summary>
internal sealed class BpmnElementActivityMap
{
    private readonly Dictionary<string, string> _elementIdByActivityId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _activityIdByElementId = new(StringComparer.Ordinal);

    private BpmnElementActivityMap()
    {
    }

    /// <summary>Reads the bindings of <paramref name="root"/> and of every scope nested in it.</summary>
    public static BpmnElementActivityMap From(JsonObject root)
    {
        var map = new BpmnElementActivityMap();
        map.AddScope(root);
        return map;
    }

    /// <summary>The element bound to the activity <paramref name="activityId"/>, or null when it is not bound work.</summary>
    public string? ElementIdOf(string activityId) => _elementIdByActivityId.GetValueOrDefault(activityId);

    /// <summary>
    /// The activity to select for the element <paramref name="elementId"/>: the activity bound to it, or, for an element
    /// with none such as a gateway, the scope that holds it. Null for an element no scope declares.
    /// </summary>
    public string? ActivityIdOf(string elementId) => _activityIdByElementId.GetValueOrDefault(elementId);

    /// <summary>
    /// Shows <paramref name="activityStats"/> and <paramref name="elementStats"/> as one overlay per element, the way the
    /// read-only canvas combines them: an element's own counts win, and a fault, cancellation or block on either shows.
    /// </summary>
    /// <param name="activityStats">State per bound activity, keyed by activity id or by node id.</param>
    /// <param name="elementStats">State per element, for gateways, events and sequence flows.</param>
    public Dictionary<string, BpmnDesignerElementStats> Overlay(IEnumerable<KeyValuePair<string, ActivityStats>> activityStats, IReadOnlyDictionary<string, BpmnElementStats> elementStats)
    {
        var activityStatsByElementId = new Dictionary<string, ActivityStats>(StringComparer.Ordinal);

        foreach (var (key, stats) in activityStats)
        {
            if (ElementIdOfStatsKey(key) is { } elementId)
                activityStatsByElementId[elementId] = stats;
        }

        var overlay = new Dictionary<string, BpmnDesignerElementStats>(StringComparer.Ordinal);

        foreach (var elementId in activityStatsByElementId.Keys.Union(elementStats.Keys))
        {
            var activity = activityStatsByElementId.GetValueOrDefault(elementId);
            var element = elementStats.GetValueOrDefault(elementId);

            overlay[elementId] = new()
            {
                Started = element?.Started ?? (int?)activity?.Started,
                Completed = element?.Completed ?? (int?)activity?.Completed,
                Active = element?.Active ?? (int?)activity?.Uncompleted,
                Blocked = element?.Blocked == true || activity?.Blocked == true ? true : element?.Blocked ?? activity?.Blocked,
                Faulted = element?.Faulted == true || activity?.Faulted == true ? true : element?.Faulted ?? activity?.Faulted,
                Canceled = element?.Canceled
            };
        }

        return overlay;
    }

    /// <summary>
    /// The element for a stats key. The report a viewer loads first keys stats by node id, the activity id prefixed by
    /// its ancestors' ids; later updates key them by activity id. Activity ids can themselves contain ':', so a node id
    /// is matched by its ending rather than split.
    /// </summary>
    private string? ElementIdOfStatsKey(string key)
    {
        if (ElementIdOf(key) is { } elementId)
            return elementId;

        var activityId = _elementIdByActivityId.Keys.Where(id => key.EndsWith(":" + id, StringComparison.Ordinal)).MaxBy(id => id.Length);
        return activityId != null ? _elementIdByActivityId[activityId] : null;
    }

    private void AddScope(JsonObject scope)
    {
        if (scope["process"] is JsonObject process && process["elements"] is JsonArray elements)
        {
            var workBindings = scope["workBindings"] as JsonObject;

            foreach (var element in elements.OfType<JsonObject>())
            {
                if (element["elementId"]?.GetValue<string>() is not { } elementId)
                    continue;

                var activityId = element["bindingRef"]?.GetValue<string>() is { } bindingRef ? workBindings?[bindingRef]?.GetValue<string>() : null;

                if (activityId != null)
                    _elementIdByActivityId[activityId] = elementId;

                _activityIdByElementId[elementId] = activityId ?? scope.GetId();
            }
        }

        foreach (var child in scope.GetActivities())
            AddScope(child);
    }
}
