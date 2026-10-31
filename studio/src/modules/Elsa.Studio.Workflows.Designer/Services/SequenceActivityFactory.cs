using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Extensions;
using Humanizer;

namespace Elsa.Studio.Workflows.Designer.Services;

/// <summary>
/// Creates child activity JSON for Sequence designers.
/// </summary>
public static class SequenceActivityFactory
{
    /// <summary>
    /// Creates a child activity for the specified Sequence at the given designer position.
    /// </summary>
    public static JsonObject CreateActivity(
        JsonObject sequence,
        ActivityDescriptor activityDescriptor,
        IIdentityGenerator identityGenerator,
        IActivityNameGenerator activityNameGenerator,
        double x,
        double y)
    {
        var activities = sequence.GetActivities().ToList();
        var newActivityId = identityGenerator.GenerateId();
        var newActivity = new JsonObject(new Dictionary<string, JsonNode?>
        {
            ["id"] = newActivityId,
            ["nodeId"] = $"{sequence.GetNodeId()}:{newActivityId}",
            ["name"] = activityNameGenerator.GenerateNextName(activities, activityDescriptor),
            ["type"] = activityDescriptor.TypeName,
            ["version"] = activityDescriptor.Version,
        });

        newActivity.SetDesignerMetadata(new()
        {
            Position = new(x, y)
        });

        foreach (var property in activityDescriptor.ConstructionProperties)
        {
            var valueNode = JsonSerializer.SerializeToNode(property.Value);
            var propertyName = property.Key.Camelize();
            newActivity.SetProperty(valueNode, propertyName);
        }

        if (activityDescriptor.Kind == ActivityKind.Trigger && activities.All(activity => activity.GetCanStartWorkflow() != true))
            newActivity.SetCanStartWorkflow(true);

        return newActivity;
    }
}
