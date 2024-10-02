using Elsa.ResourceManagement.Metadata.Builders;

namespace Elsa.ResourceManagement.Metadata.Settings;

public static class ResourceTypePartSettingsExtensions
{
    public static ResourceTypePartDefinitionBuilder WithDisplayName(this ResourceTypePartDefinitionBuilder builder, string displayName)
    {
        return builder.MergeSettings<ResourceTypePartSettings>(x => x.DisplayName = displayName);
    }

    public static ResourceTypePartDefinitionBuilder WithDescription(this ResourceTypePartDefinitionBuilder builder, string description)
    {
        return builder.MergeSettings<ResourceTypePartSettings>(x => x.Description = description);
    }

    public static ResourceTypePartDefinitionBuilder WithPosition(this ResourceTypePartDefinitionBuilder builder, string position)
    {
        return builder.MergeSettings<ResourceTypePartSettings>(x => x.Position = position);
    }

    public static ResourceTypePartDefinitionBuilder WithDisplayMode(this ResourceTypePartDefinitionBuilder builder, string displayMode)
    {
        return builder.MergeSettings<ResourceTypePartSettings>(x => x.DisplayMode = displayMode);
    }

    public static ResourceTypePartDefinitionBuilder WithEditor(this ResourceTypePartDefinitionBuilder builder, string editor)
    {
        return builder.MergeSettings<ResourceTypePartSettings>(x => x.Editor = editor);
    }
}