namespace Elsa.OrchardCore;

public static class OrchardCoreActivityNameHelper
{
    public static string GetContentItemEventActivityFullTypeName(string contentType, string eventName)
    {
        return $"OrchardCore.ContentItem.{contentType}.{eventName}";
    }
}