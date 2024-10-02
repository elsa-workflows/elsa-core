using Elsa.ResourceManagement.Metadata.Models;
using Elsa.ResourceManagement.Metadata.Settings;

namespace Elsa.ResourceManagement.Metadata;

public static class ResourcePartFieldDefinitionExtensions
{
    public static bool IsNamedPart(this ResourcePartFieldDefinition fieldDefinition)
    {
        return fieldDefinition.PartDefinition.IsReusable() && fieldDefinition.ResourceTypePartDefinition.Name != fieldDefinition.PartDefinition.Name;
    }
}