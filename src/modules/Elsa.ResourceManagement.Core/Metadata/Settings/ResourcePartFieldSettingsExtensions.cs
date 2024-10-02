using Elsa.ResourceManagement.Metadata.Builders;

namespace Elsa.ResourceManagement.Metadata.Settings;

public static class ResourcePartFieldSettingsExtensions
{
    public static ResourcePartFieldDefinitionBuilder WithDisplayName(this ResourcePartFieldDefinitionBuilder builder, string displayName)
    {
        return builder.MergeSettings<ResourcePartFieldSettings>(x => x.DisplayName = displayName);
    }

    public static ResourcePartFieldDefinitionBuilder WithDescription(this ResourcePartFieldDefinitionBuilder builder, string description)
    {
        return builder.MergeSettings<ResourcePartFieldSettings>(x => x.Description = description);
    }

    public static ResourcePartFieldDefinitionBuilder WithEditor(this ResourcePartFieldDefinitionBuilder builder, string editor)
    {
        return builder.MergeSettings<ResourcePartFieldSettings>(x => x.Editor = editor);
    }

    public static ResourcePartFieldDefinitionBuilder WithDisplayMode(this ResourcePartFieldDefinitionBuilder builder, string displayMode)
    {
        return builder.MergeSettings<ResourcePartFieldSettings>(x => x.DisplayMode = displayMode);
    }

    public static ResourcePartFieldDefinitionBuilder WithPosition(this ResourcePartFieldDefinitionBuilder builder, string position)
    {
        return builder.MergeSettings<ResourcePartFieldSettings>(x => x.Position = position);
    }
}