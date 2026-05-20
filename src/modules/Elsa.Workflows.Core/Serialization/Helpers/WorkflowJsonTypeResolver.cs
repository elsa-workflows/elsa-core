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

    private static readonly IDictionary<Type, string> GenericCollectionAliases =
        GenericCollectionTypes.ToDictionary(x => x.Value, x => x.Key);

    /// <summary>
    /// Resolves the specified workflow JSON type alias.
    /// </summary>
    public static Type ResolveType(IWellKnownTypeRegistry wellKnownTypeRegistry, string? typeAlias)
    {
        if (string.IsNullOrWhiteSpace(typeAlias))
            throw new JsonException("The workflow JSON type alias is missing.");

        if (TryResolveType(wellKnownTypeRegistry, typeAlias, out var type))
            return type;

        throw new JsonException(
            $"Unknown workflow JSON type alias '{typeAlias}'. Only registered aliases and supported compound aliases can be deserialized.");
    }

    /// <summary>
    /// Attempts to resolve the specified workflow JSON type alias.
    /// </summary>
    public static bool TryResolveType(IWellKnownTypeRegistry wellKnownTypeRegistry, string typeAlias, out Type type)
    {
        if (wellKnownTypeRegistry.TryGetType(typeAlias, out var registeredType))
        {
            type = registeredType;
            return true;
        }

        if (TryResolveArrayType(wellKnownTypeRegistry, typeAlias, out var arrayType))
        {
            type = arrayType;
            return true;
        }

        if (TryResolveGenericCollectionType(wellKnownTypeRegistry, typeAlias, out var genericCollectionType))
        {
            type = genericCollectionType;
            return true;
        }

        type = null!;
        return false;
    }

    /// <summary>
    /// Attempts to return a workflow JSON type alias that this resolver can read back.
    /// </summary>
    public static bool TryGetAlias(IWellKnownTypeRegistry wellKnownTypeRegistry, Type type, out string alias)
    {
        if (wellKnownTypeRegistry.TryGetAlias(type, out alias!))
            return true;

        if (type.IsArray)
        {
            var elementType = type.GetElementType()!;

            if (TryGetAlias(wellKnownTypeRegistry, elementType, out var elementTypeAlias))
            {
                alias = $"{elementTypeAlias}[]";
                return true;
            }
        }

        if (type is { IsGenericType: true, GenericTypeArguments.Length: 1 })
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();

            if (GenericCollectionAliases.TryGetValue(genericTypeDefinition, out var genericTypeAlias) &&
                TryGetAlias(wellKnownTypeRegistry, type.GenericTypeArguments[0], out var elementTypeAlias))
            {
                alias = $"{genericTypeAlias}<{elementTypeAlias}>";
                return true;
            }
        }

        alias = null!;
        return false;
    }

    private static bool TryResolveArrayType(IWellKnownTypeRegistry wellKnownTypeRegistry, string typeAlias, out Type type)
    {
        type = null!;

        if (!typeAlias.EndsWith("[]", StringComparison.Ordinal))
            return false;

        var elementTypeAlias = typeAlias[..^2];
        if (!TryResolveType(wellKnownTypeRegistry, elementTypeAlias, out var elementType))
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
        if (!TryResolveType(wellKnownTypeRegistry, elementTypeAlias, out var elementType))
            return false;

        type = genericTypeDefinition.MakeGenericType(elementType);
        return true;
    }
}
