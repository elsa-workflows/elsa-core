using System.Text.Json.Nodes;
using Elsa.Api.Client.Shared.Models;

namespace Elsa.Api.Client.Extensions;

/// <summary>
/// Provides extension methods for <see cref="JsonObject"/>.
/// </summary>
public static class ActivityExtensions
{
    /// <summary>
    /// Gets the type name of the specified activity.
    /// </summary>
    public static string GetTypeName(this JsonObject activity) => activity.GetProperty<string>("type")!;
    
    /// <summary>
    /// Gets the version of the specified activity.
    /// </summary>
    public static int GetVersion(this JsonObject activity) => activity.GetProperty<int>("version");

    /// <summary>
    /// Gets the ID of the specified activity.
    /// </summary>
    public static string GetId(this JsonObject activity) => activity.GetProperty<string>("id")!;

    /// <summary>
    /// Sets the ID of the specified activity.
    /// </summary>
    public static void SetId(this JsonObject activity, string value) => activity.SetProperty(JsonValue.Create(value), "id");
    
    /// <summary>
    /// Gets the name of the specified activity.
    /// </summary>
    public static string? GetName(this JsonObject activity) => activity.GetProperty<string>("name");

    /// <summary>
    /// Sets the name of the specified activity.
    /// </summary>
    public static void SetName(this JsonObject activity, string? value) => activity.SetProperty(JsonValue.Create(value), "name");

    /// <summary>
    /// Gets the designer metadata for the specified activity.
    /// </summary>
    public static JsonObject? GetMetadata(this JsonObject activity) => activity.GetProperty("metadata")?.AsObject();

    /// <summary>
    /// Sets the designer metadata for the specified activity.
    /// </summary>
    public static void SetDesignerMetadata(this JsonObject activity, ActivityDesignerMetadata designerMetadata) => activity.SetProperty(designerMetadata.SerializeToNode(), "metadata", "designer");

    /// <summary>
    /// Gets the designer metadata for the specified activity.
    /// </summary>
    public static ActivityDesignerMetadata GetDesignerMetadata(this JsonObject activity) => activity.GetProperty<ActivityDesignerMetadata>("metadata", "designer") ?? new ActivityDesignerMetadata();

    /// <summary>
    /// Gets the display text for the specified activity.
    /// </summary>
    public static string? GetDisplayText(this JsonObject activity) => activity.GetProperty("metadata", "displayText")?.GetValue<string>();

    /// <summary>
    /// Sets the display text for the specified activity.
    /// </summary>
    public static void SetDisplayText(this JsonObject activity, string? value)
    {
        activity.SetProperty(JsonValue.Create(value), "metadata", "displayText");
    }

    /// <summary>
    /// Gets the description for the specified activity.
    /// </summary>
    public static string? GetDescription(this JsonObject activity) => activity.GetProperty("metadata", "description")?.GetValue<string>();

    /// <summary>
    /// Sets the description for the specified activity.
    /// </summary>
    public static void SetDescription(this JsonObject activity, string value) => activity.SetProperty(JsonValue.Create(value), "metadata", "description");

    /// <summary>
    /// Gets a value indicating whether the description for the specified activity should be shown.
    /// </summary>
    public static bool? GetShowDescription(this JsonObject activity) => activity.GetProperty<bool>("metadata", "showDescription");

    /// <summary>
    /// Sets a value indicating whether the description for the specified activity should be shown.
    /// </summary>
    public static void SetShowDescription(this JsonObject activity, bool value) => activity.SetProperty(JsonValue.Create(value), "metadata", "showDescription");

    /// <summary>
    /// Gets a value indicating whether the specified activity can trigger the workflow.
    /// </summary>
    public static bool? GetCanStartWorkflow(this JsonObject activity) => activity.GetProperty<bool>("customProperties", "canStartWorkflow");

    /// <summary>
    /// Sets a value indicating whether the specified activity can trigger the workflow.
    /// </summary>
    public static void SetCanStartWorkflow(this JsonObject activity, bool value) => activity.SetProperty(JsonValue.Create(value), "customProperties", "canStartWorkflow");

    /// <summary>
    /// Gets the activities in the specified flowchart.
    /// </summary>
    public static IEnumerable<JsonObject> GetActivities(this JsonObject flowchart) => flowchart.GetProperty("activities")?.AsArray().AsEnumerable().Cast<JsonObject>() ?? Array.Empty<JsonObject>();

    /// <summary>
    /// Sets the activities in the specified flowchart.
    /// </summary>
    public static void SetActivities(this JsonObject flowchart, JsonArray activities) => flowchart.SetProperty(activities, "activities");

    /// <summary>
    /// Gets the connections in the specified flowchart.
    /// </summary>
    public static IEnumerable<Connection> GetConnections(this JsonObject flowchart) => flowchart.GetProperty<ICollection<Connection>>("connections") ?? new List<Connection>();

    /// <summary>
    /// Sets the connections in the specified flowchart.
    /// </summary>
    public static void SetConnections(this JsonObject flowchart, IEnumerable<Connection> connections) => flowchart.SetProperty(JsonValue.Create(connections), "connections");
    
    /// <summary>
    /// Gets the root activity in the specified activity.
    /// </summary>
    public static JsonObject? GetRoot(this JsonObject activity) => activity.GetProperty("root") as JsonObject;
}