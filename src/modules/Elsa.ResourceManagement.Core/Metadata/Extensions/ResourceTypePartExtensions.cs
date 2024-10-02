using Elsa.ResourceManagement.Metadata.Models;
using Elsa.ResourceManagement.Metadata.Settings;

namespace Elsa.ResourceManagement.Metadata;

public static class ResourceTypePartExtensions
{
    public static string DisplayName(this ResourceTypePartDefinition typePart)
    {
        var displayName = typePart.GetSettings<ResourceTypePartSettings>().DisplayName;

        if (string.IsNullOrEmpty(displayName))
        {
            // ResourceType creates a same named ResourcePart. As DisplayName is not stored in ResourcePart,
            // fetching it from the parent ResourceType
            if (typePart.PartDefinition.Name == typePart.ResourceTypeDefinition.Name)
            {
                displayName = typePart.ResourceTypeDefinition.DisplayName;
            }
            else
            {
                displayName = typePart.PartDefinition.DisplayName();
            }
        }

        return displayName;
    }

    public static string Description(this ResourceTypePartDefinition typePart)
    {
        var description = typePart.GetSettings<ResourceTypePartSettings>().Description;

        if (string.IsNullOrEmpty(description))
        {
            description = typePart.PartDefinition.Description();
        }

        return description;
    }

    public static string Editor(this ResourceTypePartDefinition typePart) => typePart.GetSettings<ResourceTypePartSettings>().Editor;
    public static string DisplayMode(this ResourceTypePartDefinition typePart) => typePart.GetSettings<ResourceTypePartSettings>().DisplayMode;
    public static bool IsNamedPart(this ResourceTypePartDefinition typePart) => typePart.PartDefinition.IsReusable() && typePart.Name != typePart.PartDefinition.Name;
}