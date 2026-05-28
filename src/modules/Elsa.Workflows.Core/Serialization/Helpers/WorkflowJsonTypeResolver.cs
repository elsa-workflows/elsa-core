using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json;
using Elsa.Expressions.Contracts;
using Elsa.Extensions;

namespace Elsa.Workflows.Serialization.Helpers;

/// <summary>
/// Resolves workflow JSON type aliases and CLR type names.
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

    public static Type ResolveType(IWellKnownTypeRegistry wellKnownTypeRegistry, string? typeAlias)
    {
        if (string.IsNullOrWhiteSpace(typeAlias))
            throw new JsonException("The workflow JSON type alias is missing.");

        if (TryResolveType(wellKnownTypeRegistry, typeAlias, out var type))
            return type;

        throw new JsonException($"Unknown workflow JSON type alias '{typeAlias}'. Registered aliases, supported compound aliases, and CLR type names can be deserialized.");
    }

    public static bool TryResolveType(IWellKnownTypeRegistry wellKnownTypeRegistry, string typeAlias, out Type type)
    {
        IReadOnlyList<Type>? registeredTypes = null;
        return TryResolveType(wellKnownTypeRegistry, typeAlias, ref registeredTypes, out type);
    }

    public static bool TryGetAlias(IWellKnownTypeRegistry wellKnownTypeRegistry, Type type, out string alias)
    {
        if (wellKnownTypeRegistry.TryGetAlias(type, out alias!))
            return true;

        if (type.IsArray && TryGetAlias(wellKnownTypeRegistry, type.GetElementType()!, out var elementTypeAlias))
        {
            alias = $"{elementTypeAlias}[]";
            return true;
        }

        if (type is { IsGenericType: true, GenericTypeArguments.Length: 1 })
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();
            if (TryGetWritableGenericCollectionAlias(genericTypeDefinition, out var genericTypeAlias) &&
                TryGetAlias(wellKnownTypeRegistry, type.GenericTypeArguments[0], out elementTypeAlias))
            {
                alias = $"{genericTypeAlias}<{elementTypeAlias}>";
                return true;
            }
        }

        alias = null!;
        return false;
    }

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

    private static bool TryResolveType(IWellKnownTypeRegistry wellKnownTypeRegistry, string typeAlias, ref IReadOnlyList<Type>? registeredTypes, out Type type)
    {
        if (wellKnownTypeRegistry.TryGetType(typeAlias, out type!))
            return true;

        if (TryResolveArrayType(wellKnownTypeRegistry, typeAlias, ref registeredTypes, out type))
            return true;

        if (TryResolveGenericCollectionType(wellKnownTypeRegistry, typeAlias, ref registeredTypes, out type))
            return true;

        if (TryResolveRegisteredLegacyTypeName(wellKnownTypeRegistry, typeAlias, ref registeredTypes, out type))
            return true;

        if (TryResolveClrTypeName(typeAlias, out type))
            return true;

        type = null!;
        return false;
    }

    private static bool TryResolveArrayType(IWellKnownTypeRegistry wellKnownTypeRegistry, string typeAlias, ref IReadOnlyList<Type>? registeredTypes, out Type type)
    {
        type = null!;
        if (!typeAlias.EndsWith("[]", StringComparison.Ordinal))
            return false;

        if (!TryResolveType(wellKnownTypeRegistry, typeAlias[..^2], ref registeredTypes, out var elementType))
            return false;

        type = elementType.MakeArrayType();
        return true;
    }

    private static bool TryResolveGenericCollectionType(IWellKnownTypeRegistry wellKnownTypeRegistry, string typeAlias, ref IReadOnlyList<Type>? registeredTypes, out Type type)
    {
        type = null!;
        var genericStart = typeAlias.IndexOf('<', StringComparison.Ordinal);

        if (genericStart <= 0 || !typeAlias.EndsWith(">", StringComparison.Ordinal))
            return false;

        if (!GenericCollectionTypes.TryGetValue(typeAlias[..genericStart], out var genericTypeDefinition))
            return false;

        if (!TryResolveType(wellKnownTypeRegistry, typeAlias[(genericStart + 1)..^1], ref registeredTypes, out var elementType))
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
            return true;

        alias = null!;
        return false;
    }

    private static bool TryResolveClrTypeName(string typeAlias, out Type type)
    {
        try
        {
            type = Type.GetType(typeAlias, false)!;
            return type != null;
        }
        catch (Exception e) when (e is ArgumentException or FileLoadException or FileNotFoundException or TypeLoadException or BadImageFormatException)
        {
            type = null!;
            return false;
        }
    }

    private static bool TryResolveRegisteredLegacyTypeName(IWellKnownTypeRegistry wellKnownTypeRegistry, string typeAlias, ref IReadOnlyList<Type>? registeredTypes, out Type type)
    {
        registeredTypes ??= wellKnownTypeRegistry.ListTypes().ToArray();
        var registeredTypeSnapshot = registeredTypes;

        type = registeredTypeSnapshot.FirstOrDefault(x =>
            string.Equals(x.GetSimpleAssemblyQualifiedName(), typeAlias, StringComparison.Ordinal) ||
            string.Equals(x.AssemblyQualifiedName, typeAlias, StringComparison.Ordinal))!;

        if (type != null)
            return true;

        if (TryResolveLegacyGenericCollectionTypeName(wellKnownTypeRegistry, typeAlias, ref registeredTypes, out type))
            return true;

        try
        {
            var resolvedType = Type.GetType(
                typeAlias,
                assemblyName => ResolveAssembly(registeredTypeSnapshot, assemblyName),
                (assembly, typeName, ignoreCase) => ResolveType(registeredTypeSnapshot, assembly, typeName, ignoreCase),
                false);

            type = resolvedType!;
            return resolvedType != null;
        }
        catch (Exception e) when (e is ArgumentException or FileLoadException)
        {
            type = null!;
            return false;
        }
    }

    private static bool TryResolveLegacyGenericCollectionTypeName(IWellKnownTypeRegistry wellKnownTypeRegistry, string typeAlias, ref IReadOnlyList<Type>? registeredTypes, out Type type)
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

            if (!TryResolveType(wellKnownTypeRegistry, typeAlias[prefix.Length..separatorIndex], ref registeredTypes, out var elementType))
                return false;

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
