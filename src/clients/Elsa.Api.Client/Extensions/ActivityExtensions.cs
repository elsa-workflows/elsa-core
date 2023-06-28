using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Models;

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
}