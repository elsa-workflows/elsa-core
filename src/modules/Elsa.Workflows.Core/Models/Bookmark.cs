using System.Text.Json.Serialization;

namespace Elsa.Workflows.Models;

/// <summary>
/// A bookmark represents a location in a workflow where the workflow can be resumed at a later time.
/// </summary>
/// <param name="id">The ID of the bookmark.</param>
/// <param name="name">The name of the bookmark.</param>
/// <param name="hash">The hash of the bookmark.</param>
/// <param name="payload">The data associated with the bookmark.</param>
/// <param name="activityId">The ID of the activity associated with the bookmark.</param>
/// <param name="activityNodeId">The ID of the activity node associated with the bookmark.</param>
/// <param name="activityInstanceId">The ID of the activity instance associated with the bookmark.</param>
/// <param name="createdAt">The date and time the bookmark was created.</param>
/// <param name="autoBurn">Whether or not the bookmark should be automatically burned.</param>
/// <param name="callbackMethodName">The name of the method on the activity class to invoke when the bookmark is resumed.</param>
/// <param name="autoComplete">Whether or not the activity should be automatically completed when the bookmark is resumed.</param>
/// <param name="metadata">Custom properties associated with the bookmark.</param>
public class Bookmark(
    string id,
    string name,
    string hash,
    object? payload,
    string activityId,
    string activityNodeId,
    string? activityInstanceId,
    DateTimeOffset createdAt,
    bool autoBurn = true,
    string? callbackMethodName = null,
    bool autoComplete = true,
    IDictionary<string, string>? metadata = null)
{
    /// <inheritdoc />
    [JsonConstructor]
    public Bookmark() : this("", "", "",  null, "", "", "", default, false)
    {
    }

    /// <summary>The ID of the bookmark.</summary>
    public string Id { get; set; } = id;

    /// <summary>The name of the bookmark.</summary>
    public string Name { get; set; } = name;

    /// <summary>The hash of the bookmark.</summary>
    public string Hash { get; set; } = hash;

    /// <summary>The data associated with the bookmark.</summary>
    public object? Payload { get; set; } = payload;

    /// <summary>The ID of the activity associated with the bookmark.</summary>
    public string ActivityId { get; set; } = activityId;

    /// <summary>The ID of the activity node associated with the bookmark.</summary>
    public string ActivityNodeId { get; set; } = activityNodeId;

    /// <summary>The ID of the activity instance associated with the bookmark.</summary>
    public string? ActivityInstanceId { get; set; } = activityInstanceId;

    /// <summary>The date and time the bookmark was created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = createdAt;

    /// <summary>Whether the bookmark should be automatically burned.</summary>
    public bool AutoBurn { get; set; } = autoBurn;

    /// <summary>The name of the method on the activity class to invoke when the bookmark is resumed.</summary>
    public string? CallbackMethodName { get; set; } = callbackMethodName;

    /// <summary>Whether the activity should be automatically completed when the bookmark is resumed.</summary>
    public bool AutoComplete { get; set; } = autoComplete;

    /// <summary>Custom properties associated with the bookmark.</summary>
    public IDictionary<string, string>? Metadata { get; set; } = metadata;
}