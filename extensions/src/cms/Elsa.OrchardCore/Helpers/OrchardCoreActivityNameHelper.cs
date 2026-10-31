namespace Elsa.OrchardCore.Helpers;

/// <summary>
/// Provides helper methods for generating activity type names for content item event activities in an Orchard Core integration.
/// </summary>
public static class OrchardCoreActivityNameHelper
{
    /// <summary>
    /// Generates the full type name for a content item event activity based on the given content type and event name.
    /// </summary>
    /// <param name="contentType">The content type of the item for which the event activity type name is being generated.</param>
    /// <param name="eventName">The specific event name associated with the content item.</param>
    /// <returns>Returns a string representing the full type name for the specified content item event activity.</returns>
    public static string GetContentItemEventActivityFullTypeName(string contentType, string eventName)
    {
        return $"OrchardCore.ContentItem.{contentType}.{eventName}";
    }
}