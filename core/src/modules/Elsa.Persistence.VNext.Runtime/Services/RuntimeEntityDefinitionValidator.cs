using Elsa.Persistence.VNext.Runtime.Models;
using Microsoft.Extensions.Options;

namespace Elsa.Persistence.VNext.Runtime.Services;

public class RuntimeEntityDefinitionValidator(IOptions<RuntimeEntityOptions> options)
{
    public void Validate(RuntimeEntityDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.Name))
            throw new InvalidOperationException("Runtime entity definition name is required.");

        if (definition.Fields.Count == 0)
            throw new InvalidOperationException($"Runtime entity definition '{definition.Name}' must declare at least one field.");

        var duplicateField = definition.Fields.GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
        if (duplicateField is not null)
            throw new InvalidOperationException($"Runtime entity definition '{definition.Name}' declares field '{duplicateField.Key}' more than once.");

        var maxIndexedFields = Math.Min(options.Value.MaxIndexedFields, RuntimeEntityPersistenceSchemaProvider.IndexedFieldSlotCount);
        if (definition.Indexes.Count > maxIndexedFields)
            throw new InvalidOperationException($"Runtime entity definition '{definition.Name}' declares {definition.Indexes.Count} indexes, but only {maxIndexedFields} runtime index slots are available.");

        var fields = definition.Fields.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var index in definition.Indexes)
        {
            if (!fields.Contains(index.FieldName))
                throw new InvalidOperationException($"Runtime entity definition '{definition.Name}' index '{index.Name}' references unknown field '{index.FieldName}'.");
        }
    }

    public void ValidateInstance(RuntimeEntityDefinition definition, RuntimeEntityInstance instance)
    {
        var dataKeys = instance.Data.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingField = definition.Fields.FirstOrDefault(x => x.IsRequired && !dataKeys.Contains(x.Name));
        if (missingField is not null)
            throw new InvalidOperationException($"Runtime entity instance '{instance.Id}' is missing required field '{missingField.Name}'.");
    }
}
