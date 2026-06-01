using Elsa.ModularPersistence.Descriptors;

namespace Elsa.ModularPersistence.Runtime;

public sealed record RuntimeStorageIndexDefinition
{
    public RuntimeStorageIndexDefinition(
        string name,
        IEnumerable<string> fieldNames,
        bool isUnique = false,
        PhysicalizationIntent physicalizationIntent = PhysicalizationIntent.PortableDocument)
    {
        Name = name;
        FieldNames = fieldNames?.ToArray() ?? throw new ArgumentNullException(nameof(fieldNames));
        IsUnique = isUnique;
        PhysicalizationIntent = physicalizationIntent;
    }

    public string Name { get; }

    public IReadOnlyList<string> FieldNames { get; }

    public bool IsUnique { get; }

    public PhysicalizationIntent PhysicalizationIntent { get; }
}
