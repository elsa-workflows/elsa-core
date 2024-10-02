using Elsa.ResourceManagement.Metadata.Models;
using Elsa.ResourceManagement.Metadata.Settings;

namespace Elsa.ResourceManagement.Metadata;

public static class ResourcePartFieldSettingsExtensions
{
    public static string DisplayName(this ResourcePartFieldDefinition partField)
    {
        var displayName = partField.GetSettings<ResourcePartFieldSettings>().DisplayName;

        if (string.IsNullOrEmpty(displayName)) 
            displayName = partField.FieldDefinition.Name;

        return displayName;
    }

    public static string Description(this ResourcePartFieldDefinition partField)
    {
        return partField.GetSettings<ResourcePartFieldSettings>().Description;
    }

    public static string Editor(this ResourcePartFieldDefinition partField)
    {
        return partField.GetSettings<ResourcePartFieldSettings>().Editor;
    }

    public static string DisplayMode(this ResourcePartFieldDefinition partField)
    {
        return partField.GetSettings<ResourcePartFieldSettings>().DisplayMode;
    }
}