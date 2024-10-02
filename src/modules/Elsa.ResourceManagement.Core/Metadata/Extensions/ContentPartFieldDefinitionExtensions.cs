using Elsa.ResourceManagement.Metadata.Models;
using Elsa.ResourceManagement.Metadata.Settings;

namespace Elsa.ResourceManagement.Metadata;

public static class ContentPartFieldDefinitionExtensions
{
    public static bool IsNamedPart(this ContentPartFieldDefinition fieldDefinition)
        => fieldDefinition.PartDefinition.IsReusable() && fieldDefinition.ContentTypePartDefinition.Name != fieldDefinition.PartDefinition.Name;
}