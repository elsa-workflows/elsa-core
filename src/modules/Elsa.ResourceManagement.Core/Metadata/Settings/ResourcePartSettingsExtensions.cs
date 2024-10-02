using Elsa.ResourceManagement.Metadata.Builders;
using Elsa.ResourceManagement.Metadata.Models;

namespace Elsa.ResourceManagement.Metadata.Settings;

public static class ResourcePartSettingsExtensions
{
    public static ResourcePartDefinitionBuilder Attachable(this ResourcePartDefinitionBuilder builder, bool attachable = true)
    {
        return builder.MergeSettings<ResourcePartSettings>(x => x.Attachable = attachable);
    }

    public static bool IsAttachable(this ResourcePartDefinition part)
    {
        return part.GetSettings<ResourcePartSettings>().Attachable;
    }

    public static ResourcePartDefinitionBuilder Reusable(this ResourcePartDefinitionBuilder builder, bool reusable = true)
    {
        return builder.MergeSettings<ResourcePartSettings>(x => x.Reusable = reusable);
    }

    public static bool IsReusable(this ResourcePartDefinition part)
    {
        return part.GetSettings<ResourcePartSettings>().Reusable;
    }

    public static ResourcePartDefinitionBuilder WithDescription(this ResourcePartDefinitionBuilder builder, string description)
    {
        return builder.MergeSettings<ResourcePartSettings>(x => x.Description = description);
    }

    public static ResourcePartDefinitionBuilder WithDisplayName(this ResourcePartDefinitionBuilder builder, string description)
    {
        return builder.MergeSettings<ResourcePartSettings>(x => x.DisplayName = description);
    }

    public static ResourcePartDefinitionBuilder WithDefaultPosition(this ResourcePartDefinitionBuilder builder, string position)
    {
        return builder.MergeSettings<ResourcePartSettings>(x => x.DefaultPosition = position);
    }

    public static string DefaultPosition(this ResourcePartDefinition part)
    {
        return part.GetSettings<ResourcePartSettings>().DefaultPosition;
    }

    public static string Description(this ResourcePartDefinition part)
    {
        return part.GetSettings<ResourcePartSettings>().Description;
    }

    public static string DisplayName(this ResourcePartDefinition part)
    {
        var displayName = part.GetSettings<ResourcePartSettings>().DisplayName;

        if (string.IsNullOrEmpty(displayName))
        {
            displayName = part.Name.TrimEnd("Part");
        }

        return displayName;
    }
}