using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Validation;

namespace Elsa.ModularPersistence.Runtime;

public static class RuntimeStorageDefinitionManifestFactory
{
    public static RuntimeStorageDefinitionPublishResult CreateManifest(RuntimeStorageDefinition definition)
    {
        var errors = ValidateDefinition(definition).ToList();
        if (errors.Count > 0)
            return RuntimeStorageDefinitionPublishResult.Failed(definition, errors);

        try
        {
            var fields = definition.Fields
                .Select(x => new StorageFieldDescriptor(x.Name, x.Type, x.IsRequired))
                .ToArray();
            var indexes = definition.Indexes
                .Select(x => new StorageIndexDescriptor(x.Name, x.FieldNames.Select(fieldName => new StorageIndexFieldDescriptor(fieldName)), x.IsUnique, x.PhysicalizationIntent))
                .ToArray();
            var manifest = new StorageManifestDescriptor(
                definition.SchemaName,
                definition.Version,
                [
                    new StorageUnitDescriptor(
                        definition.StorageUnitName,
                        fields,
                        [new StorageKeyDescriptor($"PK_{definition.StorageUnitName}", ["Id"])],
                        indexes,
                        PhysicalizationIntent.PortableDocument,
                        StorageUnitKind.Document)
                ]);

            return RuntimeStorageDefinitionPublishResult.Success(definition, manifest);
        }
        catch (Exception e) when (e is ArgumentException or ArgumentOutOfRangeException)
        {
            var path = e switch
            {
                ArgumentException argumentException => argumentException.ParamName ?? "definition",
                _ => "definition"
            };
            return RuntimeStorageDefinitionPublishResult.Failed(definition, [Error("InvalidDescriptor", e.Message, path)]);
        }
    }

    private static IEnumerable<StorageManifestValidationError> ValidateDefinition(RuntimeStorageDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.SchemaName))
            yield return Error("MissingSchemaName", "Runtime storage definitions require a schema name.", "schemaName");

        if (string.IsNullOrWhiteSpace(definition.StorageUnitName))
            yield return Error("MissingStorageUnitName", "Runtime storage definitions require a storage unit name.", "storageUnitName");

        if (definition.Fields.Count == 0)
            yield return Error("MissingFields", "Runtime storage definitions require at least one field.", "fields");

        if (definition.Fields.All(x => !string.Equals(x.Name, "Id", StringComparison.Ordinal)))
            yield return Error("MissingIdField", "Runtime storage definitions must declare an Id field for portable document identity.", "fields");

        if (definition.RequiredPermissions.Count == 0)
            yield return Error("MissingPermissionRequirement", "Runtime storage definitions must declare at least one required permission.", "requiredPermissions");

        for (var i = 0; i < definition.RequiredPermissions.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(definition.RequiredPermissions[i]))
                yield return Error("InvalidPermissionRequirement", "Runtime storage definition permission requirements cannot be blank.", $"requiredPermissions[{i}]");
        }
    }

    private static StorageManifestValidationError Error(string code, string message, string path) =>
        new(code, message, path);
}
