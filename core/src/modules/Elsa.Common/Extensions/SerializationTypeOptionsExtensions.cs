using Elsa.Common.Serialization;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extends <see cref="SerializationTypeOptions"/>.
/// </summary>
public static class SerializationTypeOptionsExtensions
{
    /// <summary>
    /// Registers a preferred serialization alias.
    /// </summary>
    public static SerializationTypeOptions AddTypeAlias<T>(this SerializationTypeOptions options, string alias) => options.RegisterTypeAlias(typeof(T), alias);

    /// <summary>
    /// Registers the type name as the preferred serialization alias.
    /// </summary>
    public static SerializationTypeOptions AddTypeAlias<T>(this SerializationTypeOptions options) => options.RegisterTypeAlias(typeof(T), typeof(T).Name);

    /// <summary>
    /// Registers a preferred serialization alias and the current simple assembly-qualified name as a legacy identifier.
    /// </summary>
    public static SerializationTypeOptions AddTypeAliasWithLegacyName(this SerializationTypeOptions options, Type type, string alias)
    {
        options.RegisterTypeAlias(type, alias);
        options.RegisterLegacySimpleAssemblyQualifiedName(type);
        return options;
    }

    /// <summary>
    /// Registers a preferred serialization alias and the current simple assembly-qualified name as a legacy identifier.
    /// </summary>
    public static SerializationTypeOptions AddTypeAliasWithLegacyName<T>(this SerializationTypeOptions options, string alias) => options.AddTypeAliasWithLegacyName(typeof(T), alias);

    /// <summary>
    /// Registers the current simple assembly-qualified name as a compatibility identifier.
    /// </summary>
    public static SerializationTypeOptions AddLegacySimpleAssemblyQualifiedName<T>(this SerializationTypeOptions options) => options.RegisterLegacySimpleAssemblyQualifiedName(typeof(T));

    /// <summary>
    /// Registers the current simple assembly-qualified name as a compatibility identifier.
    /// </summary>
    public static SerializationTypeOptions AddLegacySimpleAssemblyQualifiedName(this SerializationTypeOptions options, Type type) => options.RegisterLegacySimpleAssemblyQualifiedName(type);

    /// <summary>
    /// Registers the current simple assembly-qualified name as the preferred alias for compatibility-only types.
    /// </summary>
    public static SerializationTypeOptions AddSimpleAssemblyQualifiedTypeAlias(this SerializationTypeOptions options, Type type)
    {
        return options.RegisterTypeAlias(type, type.GetSimpleAssemblyQualifiedName());
    }
}
