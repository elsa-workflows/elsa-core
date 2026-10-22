using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Resolvers;

/// <summary>
/// Resolves version-one forward and handler children by stable StepId and role.
/// </summary>
public sealed class CompensationScopeActivityResolver : IActivityResolver
{
    public int Priority => 0;

    public bool GetSupportsActivity(JsonObject activity) =>
        activity.GetTypeName() == "Elsa.CompensationScope" || NativeCompensationDraftCapability.IsOpaqueCompensationScope(activity);

    public ValueTask<IEnumerable<EmbeddedActivity>> GetActivitiesAsync(JsonObject activity, CancellationToken cancellationToken = default)
    {
        if (activity.GetTypeName() != "Elsa.CompensationScope" ||
            !HasSupportedSchema(activity) || activity["steps"] is not JsonArray steps)
            return new(Array.Empty<EmbeddedActivity>());

        var children = new List<EmbeddedActivity>();
        foreach (var step in steps.OfType<JsonObject>())
        {
            var stepId = GetString(step, "stepId");
            AddChild("forward");
            AddChild("handler");

            void AddChild(string role)
            {
                if (step[role] is JsonObject child && child.IsActivity())
                    children.Add(new(child, "steps") { StepId = stepId, Role = role });
            }
        }

        return new(children);
    }

    private static bool HasSupportedSchema(JsonObject scope) =>
        scope["version"] is JsonValue version && version.TryGetValue<int>(out var activityVersion) && activityVersion == 1 &&
        scope["draftSchemaVersion"] is JsonValue schema && schema.TryGetValue<int>(out var schemaVersion) && schemaVersion == 1;

    private static string? GetString(JsonObject value, string propertyName) =>
        value[propertyName] is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var result) ? result : null;
}
