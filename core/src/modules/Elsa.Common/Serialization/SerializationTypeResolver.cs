using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json;
using Elsa.Extensions;

namespace Elsa.Common.Serialization;

/// <summary>
/// Resolves serialization type aliases without loading arbitrary CLR type names.
/// </summary>
public static class SerializationTypeResolver
{
    private const int MaxAliasLength = 4096;
    private const int MaxAliasComponents = 64;

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

    private static readonly IDictionary<Type, string> GenericCollectionAliases = new Dictionary<Type, string>
    {
        [typeof(List<>)] = "List",
        [typeof(HashSet<>)] = "HashSet",
        [typeof(Collection<>)] = "Collection"
    };

    private static readonly IDictionary<Type, Type> GenericCollectionInterfaceMappings = new Dictionary<Type, Type>
    {
        [typeof(IEnumerable<>)] = typeof(List<>),
        [typeof(ICollection<>)] = typeof(List<>),
        [typeof(IList<>)] = typeof(List<>),
        [typeof(IReadOnlyCollection<>)] = typeof(List<>),
        [typeof(IReadOnlyList<>)] = typeof(List<>),
        [typeof(ISet<>)] = typeof(HashSet<>),
        [typeof(IDictionary<,>)] = typeof(Dictionary<,>),
        [typeof(IReadOnlyDictionary<,>)] = typeof(Dictionary<,>)
    };

    private static readonly IDictionary<Type, Type> CollectionInterfaceMappings = new Dictionary<Type, Type>
    {
        [typeof(IEnumerable)] = typeof(List<object>),
        [typeof(ICollection)] = typeof(List<object>),
        [typeof(IList)] = typeof(List<object>),
        [typeof(IDictionary)] = typeof(Dictionary<string, object>)
    };

    /// <summary>
    /// Resolves the specified serialization type alias.
    /// </summary>
    public static Type ResolveType(ISerializationTypeRegistry serializationTypeRegistry, string? typeAlias)
    {
        if (typeAlias == null || typeAlias.Length == 0)
            throw new JsonException("The serialization type alias is missing.");

        if (!IsWithinAliasBudget(typeAlias))
            throw new JsonException("The serialization type alias exceeds the maximum length or type composition limit.");

        if (string.IsNullOrWhiteSpace(typeAlias))
            throw new JsonException("The serialization type alias is missing.");

        IReadOnlyList<Type>? registeredTypes = null;
        if (TryResolveType(serializationTypeRegistry, typeAlias, ref registeredTypes, out var type))
            return type;

        throw new JsonException(
            $"Unknown serialization type alias '{typeAlias}'. Only registered aliases and supported compound aliases can be deserialized.");
    }

    /// <summary>
    /// Attempts to resolve the specified serialization type alias.
    /// </summary>
    public static bool TryResolveType(ISerializationTypeRegistry serializationTypeRegistry, string typeAlias, out Type type)
    {
        type = null!;
        IReadOnlyList<Type>? registeredTypes = null;
        return IsWithinAliasBudget(typeAlias) && TryResolveType(serializationTypeRegistry, typeAlias, ref registeredTypes, out type);
    }

    private static bool IsWithinAliasBudget(string typeAlias)
    {
        if (typeAlias.Length > MaxAliasLength)
            return false;

        // Count both the alias syntax and the modifiers used by legacy CLR names before
        // either our recursive resolver or the CLR type-name parser sees the input.
        var components = 0;
        foreach (var character in typeAlias)
        {
            if (character is '<' or '[' or '*' or '&' && ++components > MaxAliasComponents)
                return false;
        }

        return true;
    }

    private static bool TryResolveType(ISerializationTypeRegistry serializationTypeRegistry, string typeAlias, ref IReadOnlyList<Type>? registeredTypes, out Type type)
    {
        if (serializationTypeRegistry.TryGetType(typeAlias, out var registeredType))
        {
            type = registeredType;
            return true;
        }

        if (TryResolveArrayType(serializationTypeRegistry, typeAlias, ref registeredTypes, out var arrayType))
        {
            type = arrayType;
            return true;
        }

        if (TryResolveGenericCollectionType(serializationTypeRegistry, typeAlias, ref registeredTypes, out var genericCollectionType))
        {
            type = genericCollectionType;
            return true;
        }

        if (TryResolveRegisteredLegacyTypeName(serializationTypeRegistry, typeAlias, ref registeredTypes, out var legacyType))
        {
            type = legacyType;
            return true;
        }

        type = null!;
        return false;
    }

    /// <summary>
    /// Attempts to return a serialization type alias that this resolver can read back.
    /// </summary>
    public static bool TryGetAlias(ISerializationTypeRegistry serializationTypeRegistry, Type type, out string alias)
    {
        if (TryBuildAlias(serializationTypeRegistry, type, MaxAliasComponents, out alias) && IsWithinAliasBudget(alias))
            return true;

        alias = null!;
        return false;
    }

    private static bool TryBuildAlias(ISerializationTypeRegistry serializationTypeRegistry, Type type, int remainingComponents, out string alias)
    {
        if (serializationTypeRegistry.TryGetAlias(type, out alias!))
            return true;

        if (remainingComponents == 0)
        {
            alias = null!;
            return false;
        }

        if (type.IsArray)
        {
            var elementType = type.GetElementType()!;

            if (TryBuildAlias(serializationTypeRegistry, elementType, remainingComponents - 1, out var elementTypeAlias))
            {
                alias = $"{elementTypeAlias}[]";
                return true;
            }
        }

        if (type is { IsGenericType: true, GenericTypeArguments.Length: 1 })
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();

            if (TryGetWritableGenericCollectionAlias(genericTypeDefinition, out var genericTypeAlias) &&
                TryBuildAlias(serializationTypeRegistry, type.GenericTypeArguments[0], remainingComponents - 1, out var elementTypeAlias))
            {
                alias = $"{genericTypeAlias}<{elementTypeAlias}>";
                return true;
            }
        }

        alias = null!;
        return false;
    }

    /// <summary>
    /// Attempts to map a supported collection interface type to an instantiable concrete type.
    /// </summary>
    public static bool TryGetInstantiableCollectionType(Type type, out Type instantiableType)
    {
        if (type.IsGenericType)
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();
            if (GenericCollectionInterfaceMappings.TryGetValue(genericTypeDefinition, out var instantiableGenericTypeDefinition))
            {
                instantiableType = instantiableGenericTypeDefinition.MakeGenericType(type.GenericTypeArguments);
                return true;
            }
        }

        if (CollectionInterfaceMappings.TryGetValue(type, out instantiableType!))
            return true;

        instantiableType = null!;
        return false;
    }

    private static bool TryResolveArrayType(ISerializationTypeRegistry serializationTypeRegistry, string typeAlias, ref IReadOnlyList<Type>? registeredTypes, out Type type)
    {
        type = null!;

        if (!typeAlias.EndsWith("[]", StringComparison.Ordinal))
            return false;

        var elementTypeAlias = typeAlias[..^2];
        if (!TryResolveType(serializationTypeRegistry, elementTypeAlias, ref registeredTypes, out var elementType))
            return false;

        type = elementType.MakeArrayType();
        return true;
    }

    private static bool TryResolveGenericCollectionType(ISerializationTypeRegistry serializationTypeRegistry, string typeAlias, ref IReadOnlyList<Type>? registeredTypes, out Type type)
    {
        type = null!;
        var genericStart = typeAlias.IndexOf('<', StringComparison.Ordinal);

        if (genericStart <= 0 || !typeAlias.EndsWith(">", StringComparison.Ordinal))
            return false;

        var genericTypeAlias = typeAlias[..genericStart];
        if (!GenericCollectionTypes.TryGetValue(genericTypeAlias, out var genericTypeDefinition))
            return false;

        var elementTypeAlias = typeAlias[(genericStart + 1)..^1];
        if (!TryResolveType(serializationTypeRegistry, elementTypeAlias, ref registeredTypes, out var elementType))
            return false;

        type = genericTypeDefinition.MakeGenericType(elementType);
        return true;
    }

    private static bool TryGetWritableGenericCollectionAlias(Type genericTypeDefinition, out string alias)
    {
        if (GenericCollectionAliases.TryGetValue(genericTypeDefinition, out alias!))
            return true;

        if (GenericCollectionInterfaceMappings.TryGetValue(genericTypeDefinition, out var instantiableGenericTypeDefinition) &&
            GenericCollectionAliases.TryGetValue(instantiableGenericTypeDefinition, out alias!))
        {
            return true;
        }

        alias = null!;
        return false;
    }

    private static bool TryResolveRegisteredLegacyTypeName(ISerializationTypeRegistry serializationTypeRegistry, string typeAlias, ref IReadOnlyList<Type>? registeredTypes, out Type type)
    {
        registeredTypes ??= GetRegisteredTypes(serializationTypeRegistry);
        var registeredTypeSnapshot = registeredTypes;

        if (TryResolveRegisteredSimpleAssemblyQualifiedName(registeredTypeSnapshot, typeAlias, out type))
            return true;

        if (TryResolveLegacyGenericCollectionTypeName(serializationTypeRegistry, typeAlias, ref registeredTypes, out type))
            return true;

        Type? resolvedType;

        try
        {
            resolvedType = Type.GetType(
                typeAlias,
                assemblyName => ResolveAssembly(registeredTypeSnapshot, assemblyName),
                (assembly, typeName, ignoreCase) => ResolveType(registeredTypeSnapshot, assembly, typeName, ignoreCase),
                false);
        }
        catch (Exception e) when (e is ArgumentException or FileLoadException)
        {
            resolvedType = null;
        }

        type = resolvedType!;
        return resolvedType != null;
    }

    private static IReadOnlyList<Type> GetRegisteredTypes(ISerializationTypeRegistry serializationTypeRegistry)
    {
        return serializationTypeRegistry.ListTypes().ToArray();
    }

    private static bool TryResolveRegisteredSimpleAssemblyQualifiedName(IEnumerable<Type> registeredTypes, string typeAlias, out Type type)
    {
        type = registeredTypes.FirstOrDefault(x =>
            string.Equals(x.GetSimpleAssemblyQualifiedName(), typeAlias, StringComparison.Ordinal) ||
            string.Equals(x.AssemblyQualifiedName, typeAlias, StringComparison.Ordinal))!;

        return type != null;
    }

    private static bool TryResolveLegacyGenericCollectionTypeName(ISerializationTypeRegistry serializationTypeRegistry, string typeAlias, ref IReadOnlyList<Type>? registeredTypes, out Type type)
    {
        type = null!;

        foreach (var genericTypeDefinition in GenericCollectionTypes.Values)
        {
            var prefix = $"{genericTypeDefinition.FullName}[[";
            var separatorIndex = typeAlias.LastIndexOf("]], ", StringComparison.Ordinal);

            if (!typeAlias.StartsWith(prefix, StringComparison.Ordinal) || separatorIndex <= prefix.Length)
                continue;

            var assemblyName = typeAlias[(separatorIndex + 4)..].Split(',')[0];
            if (!string.Equals(assemblyName, genericTypeDefinition.Assembly.GetName().Name, StringComparison.Ordinal))
                continue;

            var elementTypeAlias = typeAlias[prefix.Length..separatorIndex];
            if (!TryResolveType(serializationTypeRegistry, elementTypeAlias, ref registeredTypes, out var elementType))
                return false;

            // The resolver only closes known collection definitions over registered element types.
#pragma warning disable IL2055
            type = genericTypeDefinition.MakeGenericType(elementType);
#pragma warning restore IL2055
            return true;
        }

        return false;
    }

    private static Assembly? ResolveAssembly(IEnumerable<Type> registeredTypes, AssemblyName assemblyName)
    {
        var coreLibAssembly = typeof(List<>).Assembly;
        if (AssemblyName.ReferenceMatchesDefinition(coreLibAssembly.GetName(), assemblyName))
            return coreLibAssembly;

        return registeredTypes
            .Select(x => x.Assembly)
            .Distinct()
            .FirstOrDefault(x => AssemblyName.ReferenceMatchesDefinition(x.GetName(), assemblyName));
    }

    private static Type? ResolveType(IEnumerable<Type> registeredTypes, Assembly? assembly, string typeName, bool ignoreCase)
    {
        if (assembly == typeof(List<>).Assembly)
        {
            var genericCollectionType = GenericCollectionTypes.Values.FirstOrDefault(x =>
                x.FullName != null && string.Equals(x.FullName, typeName, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));

            if (genericCollectionType != null)
                return genericCollectionType;
        }

        return registeredTypes.FirstOrDefault(x =>
            x.Assembly == assembly &&
            x.FullName != null &&
            (string.Equals(x.FullName, typeName, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) ||
             string.Equals(x.FullName.Replace('+', '.'), typeName, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)));
    }
}
