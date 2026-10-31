using System.Text.Json.Nodes;
using Elsa.AI.Abstractions.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;

namespace Elsa.AI.Host.Services;

public class WorkflowGroundingMapper
{
    public WorkflowGroundingSummary Map(WorkflowDefinitionSummary summary) =>
        new()
        {
            Id = summary.Id,
            DefinitionId = summary.DefinitionId,
            Name = summary.Name,
            Description = summary.Description,
            Version = summary.Version,
            IsLatest = summary.IsLatest,
            IsPublished = summary.IsPublished,
            IsReadonly = summary.IsReadonly,
            MaterializerName = summary.MaterializerName,
            ProviderName = summary.ProviderName,
            CreatedAt = summary.CreatedAt
        };

    public WorkflowGroundingSummary Map(WorkflowDefinition definition)
    {
        var graph = GetGraph(definition);
        return new WorkflowGroundingSummary
        {
            Id = definition.Id,
            DefinitionId = definition.DefinitionId,
            Name = definition.Name,
            Description = definition.Description,
            Version = definition.Version,
            IsLatest = definition.IsLatest,
            IsPublished = definition.IsPublished,
            IsReadonly = definition.IsReadonly,
            MaterializerName = definition.MaterializerName,
            ProviderName = definition.ProviderName,
            CreatedAt = definition.CreatedAt,
            ActivityTypes = graph.ActivityTypes,
            Variables = definition.Variables.Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToList(),
            Inputs = definition.Inputs.Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToList(),
            Outputs = definition.Outputs.Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToList()
        };
    }

    public JsonObject MapGraph(WorkflowDefinition definition)
    {
        var graph = GetGraph(definition);
        return new JsonObject
        {
            ["definitionId"] = definition.DefinitionId,
            ["versionId"] = definition.Id,
            ["version"] = definition.Version,
            ["activityTypes"] = AIGroundingJson.ToJsonArray(graph.ActivityTypes),
            ["activityCount"] = graph.ActivityCount,
            ["activities"] = AIGroundingJson.ToJsonArray(graph.Activities),
            ["variables"] = AIGroundingJson.ToJsonArray(definition.Variables.Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x))),
            ["inputs"] = AIGroundingJson.ToJsonArray(definition.Inputs.Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x))),
            ["outputs"] = AIGroundingJson.ToJsonArray(definition.Outputs.Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x)))
        };
    }

    public WorkflowGraphSummary GetGraph(WorkflowDefinition definition)
    {
        var root = TryParseWorkflowJson(definition);
        var activities = root == null ? [] : FindActivities(root).Take(200).ToList();
        var activityTypes = activities
            .Select(x => x.Type)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new WorkflowGraphSummary(activities, activityTypes, activities.Count);
    }

    private static JsonNode? TryParseWorkflowJson(WorkflowDefinition definition)
    {
        var json = !string.IsNullOrWhiteSpace(definition.OriginalSource) ? definition.OriginalSource : definition.StringData;
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonNode.Parse(json);
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<WorkflowActivitySummary> FindActivities(JsonNode node)
    {
        if (node is JsonObject jsonObject)
        {
            var type = ReadString(jsonObject, "type") ?? ReadString(jsonObject, "typeName") ?? ReadString(jsonObject, "activityType");
            var id = ReadString(jsonObject, "id") ?? ReadString(jsonObject, "activityId") ?? ReadString(jsonObject, "nodeId");
            if (!string.IsNullOrWhiteSpace(type))
                yield return new WorkflowActivitySummary(id, type, ReadString(jsonObject, "name") ?? ReadString(jsonObject, "displayName"));

            foreach (var child in jsonObject.Select(x => x.Value).OfType<JsonNode>())
            {
                foreach (var activity in FindActivities(child))
                    yield return activity;
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (var child in jsonArray.OfType<JsonNode>())
            {
                foreach (var activity in FindActivities(child))
                    yield return activity;
            }
        }
    }

    private static string? ReadString(JsonObject jsonObject, string name) =>
        jsonObject.TryGetPropertyValue(name, out var node) && node is JsonValue value && value.TryGetValue<string>(out var result) ? result : null;
}

public record WorkflowGraphSummary(IReadOnlyCollection<WorkflowActivitySummary> Activities, IReadOnlyCollection<string> ActivityTypes, int ActivityCount);

public record WorkflowActivitySummary(string? Id, string Type, string? Name);
