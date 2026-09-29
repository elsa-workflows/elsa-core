using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Resolvers;

/// <summary>
/// Resolves child activities embedded in StateMachine state and transition slots.
/// </summary>
public class StateMachineActivityResolver : IActivityResolver
{
    private static readonly string[] StateActivitySlots = ["entry", "exit"];
    private static readonly string[] TransitionActivitySlots = ["trigger", "action"];

    /// <inheritdoc />
    public int Priority => 0;

    /// <inheritdoc />
    public bool GetSupportsActivity(JsonObject activity) => activity.GetTypeName() == "Elsa.StateMachine";

    /// <inheritdoc />
    public ValueTask<IEnumerable<EmbeddedActivity>> GetActivitiesAsync(JsonObject activity, CancellationToken cancellationToken = default)
    {
        var activities = new List<EmbeddedActivity>();

        AddSlotActivities(activities, activity["states"] as JsonArray, StateActivitySlots);
        AddSlotActivities(activities, activity["transitions"] as JsonArray, TransitionActivitySlots);

        return new(activities);
    }

    private static void AddSlotActivities(ICollection<EmbeddedActivity> activities, JsonArray? items, IEnumerable<string> slotNames)
    {
        if (items == null)
            return;

        foreach (var item in items.OfType<JsonObject>())
        {
            foreach (var slotName in slotNames)
            {
                if (item[slotName] is JsonObject slotActivity && slotActivity.IsActivity())
                    activities.Add(new(slotActivity, slotName));
            }
        }
    }
}
