using Elsa.ResourceManagement.Metadata.Models;
using Elsa.ResourceManagement.Metadata.Settings;

namespace Elsa.ResourceManagement.Metadata;

public static class ResourceTypeExtensions
{
    public static bool TryGetStereotype(this ResourceTypeDefinition resourceTypeDefinition, out string stereotype)
    {
        stereotype = resourceTypeDefinition.GetStereotype();
        return !string.IsNullOrWhiteSpace(stereotype);
    }

    public static bool HasStereotype(this ResourceTypeDefinition resourceTypeDefinition) => !string.IsNullOrEmpty(resourceTypeDefinition.GetStereotype());
    public static bool StereotypeEquals(this ResourceTypeDefinition resourceTypeDefinition, string stereotype) => resourceTypeDefinition.StereotypeEquals(stereotype, StringComparison.Ordinal);

    public static bool StereotypeEquals(this ResourceTypeDefinition resourceTypeDefinition, string stereotype, StringComparison stringComparison)
    {
        return resourceTypeDefinition.TryGetStereotype(out var st) && string.Equals(st, stereotype, stringComparison);
    }

    public static string GetStereotype(this ResourceTypeDefinition resourceTypeDefinition) => resourceTypeDefinition.GetSettings().Stereotype;
    public static bool IsListable(this ResourceTypeDefinition resourceTypeDefinition) => resourceTypeDefinition.GetSettings().Listable;
    public static bool IsCreatable(this ResourceTypeDefinition resourceTypeDefinition) => resourceTypeDefinition.GetSettings().Creatable;
    public static bool IsDraftable(this ResourceTypeDefinition resourceTypeDefinition) => resourceTypeDefinition.GetSettings().Draftable;
    public static bool IsVersionable(this ResourceTypeDefinition resourceTypeDefinition) => resourceTypeDefinition.GetSettings().Versionable;
    public static bool IsSecurable(this ResourceTypeDefinition resourceTypeDefinition) => resourceTypeDefinition.GetSettings().Securable;
    public static bool HasDescription(this ResourceTypeDefinition resourceTypeDefinition) => !string.IsNullOrWhiteSpace(resourceTypeDefinition.GetSettings().Description);
    public static string GetDescription(this ResourceTypeDefinition resourceTypeDefinition) => resourceTypeDefinition.GetSettings().Description;
    public static ResourceTypeSettings GetSettings(this ResourceTypeDefinition resourceTypeDefinition) => resourceTypeDefinition.GetSettings<ResourceTypeSettings>();
}
