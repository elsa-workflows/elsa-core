using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Shared.Models;

namespace Elsa.Api.Client.Extensions;

/// <summary>
/// Provides extension methods for <see cref="Activity"/>.
/// </summary>
public static class ActivityExtensions
{
    /// <summary>
    /// Sets the designer metadata for the specified activity.
    /// </summary>
    public static void SetDesignerMetadata(this Activity activity, ActivityDesignerMetadata designerMetadata)
    {
        var metadata = activity.Metadata;
        metadata["designer"] = designerMetadata;
        activity.Metadata = metadata;
    }

    /// <summary>
    /// Gets the designer metadata for the specified activity.
    /// </summary>
    public static ActivityDesignerMetadata GetDesignerMetadata(this Activity activity)
    {
        var metadata = activity.Metadata;
        var designerMetadata = metadata.TryGetValue("designer", () => new ActivityDesignerMetadata())!;
        metadata["designer"] = designerMetadata;
        activity.Metadata = metadata;
        return designerMetadata;
    }
    
    /// <summary>
    /// Gets the display text for the specified activity.
    /// </summary>
    public static string? GetDisplayText(this Activity activity)
    {
        var metadata = activity.Metadata;
        return metadata.TryGetValue<string>("displayText");
    }
    
    /// <summary>
    /// Sets the display text for the specified activity.
    /// </summary>
    public static void SetDisplayText(this Activity activity, string value)
    {
        var metadata = activity.Metadata;
        metadata["displayText"] = value;
        activity.Metadata = metadata;
    }
    
    /// <summary>
    /// Gets the description for the specified activity.
    /// </summary>
    public static string? GetDescription(this Activity activity)
    {
        var metadata = activity.Metadata;
        return metadata.TryGetValue<string>("description");
    }
    
    /// <summary>
    /// Sets the description for the specified activity.
    /// </summary>
    public static void SetDescription(this Activity activity, string value)
    {
        var metadata = activity.Metadata;
        metadata["description"] = value;
        activity.Metadata = metadata;
    }
    
    /// <summary>
    /// Gets a value indicating whether the description for the specified activity should be shown.
    /// </summary>
    public static bool? GetShowDescription(this Activity activity)
    {
        var metadata = activity.Metadata;
        return metadata.TryGetValue<bool>("showDescription");
    }
    
    /// <summary>
    /// Sets a value indicating whether the description for the specified activity should be shown.
    /// </summary>
    public static void SetShowDescription(this Activity activity, bool value)
    {
        var metadata = activity.Metadata;
        metadata["showDescription"] = value;
        activity.Metadata = metadata;
    }
    
    /// <summary>
    /// Gets a value indicating whether the specified activity can trigger the workflow.
    /// </summary>
    public static bool? GetCanStartWorkflow(this Activity activity)
    {
        var properties = activity.CustomProperties;
        return properties.TryGetValue<bool>("canStartWorkflow");
    }
    
    /// <summary>
    /// Sets a value indicating whether the specified activity can trigger the workflow.
    /// </summary>
    public static void SetCanStartWorkflow(this Activity activity, bool value)
    {
        var properties = activity.CustomProperties;
        properties["canStartWorkflow"] = value;
        activity.CustomProperties = properties;
    }
}