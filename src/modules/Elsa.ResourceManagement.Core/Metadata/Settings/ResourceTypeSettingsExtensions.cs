using Elsa.ResourceManagement.Metadata.Builders;

namespace Elsa.ResourceManagement.Metadata.Settings;

public static class ResourceTypeSettingsExtensions
{
    public static ResourceTypeDefinitionBuilder Creatable(this ResourceTypeDefinitionBuilder builder, bool creatable = true)
    {
        return builder.MergeSettings<ResourceTypeSettings>(x => x.Creatable = creatable);
    }

    public static ResourceTypeDefinitionBuilder Listable(this ResourceTypeDefinitionBuilder builder, bool listable = true)
    {
        return builder.MergeSettings<ResourceTypeSettings>(x => x.Listable = listable);
    }

    public static ResourceTypeDefinitionBuilder Draftable(this ResourceTypeDefinitionBuilder builder, bool draftable = true)
    {
        return builder.MergeSettings<ResourceTypeSettings>(x => x.Draftable = draftable);
    }

    public static ResourceTypeDefinitionBuilder Versionable(this ResourceTypeDefinitionBuilder builder, bool versionable = true)
    {
        return builder.MergeSettings<ResourceTypeSettings>(x => x.Versionable = versionable);
    }

    public static ResourceTypeDefinitionBuilder Securable(this ResourceTypeDefinitionBuilder builder, bool securable = true)
    {
        return builder.MergeSettings<ResourceTypeSettings>(x => x.Securable = securable);
    }

    public static ResourceTypeDefinitionBuilder Stereotype(this ResourceTypeDefinitionBuilder builder, string stereotype)
    {
        return builder.MergeSettings<ResourceTypeSettings>(x => x.Stereotype = stereotype);
    }

    public static ResourceTypeDefinitionBuilder WithDescription(this ResourceTypeDefinitionBuilder builder, string description)
    {
        return builder.MergeSettings<ResourceTypeSettings>(x => x.Description = description);
    }
}