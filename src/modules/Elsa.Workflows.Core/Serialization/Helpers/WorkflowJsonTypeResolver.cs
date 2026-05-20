using System.Collections.ObjectModel;
using System.Text.Json;
using Elsa.Expressions.Contracts;

namespace Elsa.Workflows.Serialization.Helpers;

/// <summary>
/// Resolves workflow JSON type aliases without loading arbitrary CLR type names.
/// </summary>
public static class WorkflowJsonTypeResolver
{
    private static readonly IDictionary<string, Type> GenericCollectionTypes = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        ["IEnumerable"] = typeof(IEnumerable<>),
        ["ICollection"] = typeof(ICollection<>),
        ["IList"] = typeof(IList<>),
        ["IReadOnlyCollection"] = typeof(IReadOnlyCollection<>),
        ["IReadOnlyList"] = typeof(IReadOnlyList<>),
        ["ISet"] = typeof(ISet<>),
        ["List"] = typeof(List<>),
        ["HashSet"] = typeof(HashSet<>),
        ["Collection"] = typeof(Collection<>)
    };

    /// <summary>
    /// Resolves the specified workflow JSON type alias.
    /// </summary>
    public static Type ResolveType(IWellKnownTypeRegistry wellKnownTypeRegistry, string? typeAlias)
    {
        if (string.IsNullOrWhiteSpace(typeAlias))
            throw new JsonException("The workflow JSON type alias is missing.");

        if (wellKnownTypeRegistry.TryGetType(typeAlias, out var registeredType))
            return registeredType;

        if (TryResolveArrayType(wellKnownTypeRegistry, typeAlias, out var arrayType))
            return arrayType;

        if (TryResolveGenericCollectionType(wellKnownTypeRegistry, typeAlias, out var genericCollectionType))
            return genericCollectionType;

        throw new JsonException($"Unknown workflow JSON type alias '{typeAlias}'.");
    }

    private static bool TryResolveArrayType(IWellKnownTypeRegistry wellKnownTypeRegistry, string typeAlias, out Type type)
    {
        type = null!;

        if (!typeAlias.EndsWith("[]", StringComparison.Ordinal))
            return false;

        var elementTypeAlias = typeAlias[..^2];
        if (!wellKnownTypeRegistry.TryGetType(elementTypeAlias, out var elementType))
            return false;

        type = elementType.MakeArrayType();
        return true;
    }

    private static bool TryResolveGenericCollectionType(IWellKnownTypeRegistry wellKnownTypeRegistry, string typeAlias, out Type type)
    {
        type = null!;
        var genericStart = typeAlias.IndexOf('<', StringComparison.Ordinal);

        if (genericStart <= 0 || !typeAlias.EndsWith('>'))
            return false;

        var genericTypeAlias = typeAlias[..genericStart];
        if (!GenericCollectionTypes.TryGetValue(genericTypeAlias, out var genericTypeDefinition))
            return false;

        var elementTypeAlias = typeAlias[(genericStart + 1)..^1];
        if (!wellKnownTypeRegistry.TryGetType(elementTypeAlias, out var elementType))
            return false;

        type = genericTypeDefinition.MakeGenericType(elementType);
        return true;
    }
}
