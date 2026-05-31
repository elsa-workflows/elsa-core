using Elsa.Extensions;
using Elsa.Workflows.Options;

namespace Elsa.Workflows.Extensions;

/// <summary>
/// Extends <see cref="WorkflowJsonTypeOptions"/>.
/// </summary>
public static class WorkflowJsonTypeOptionsExtensions
{
    /// <summary>
    /// Registers a preferred workflow JSON alias.
    /// </summary>
    public static WorkflowJsonTypeOptions AddTypeAlias<T>(this WorkflowJsonTypeOptions options, string alias) => options.RegisterTypeAlias(typeof(T), alias);

    /// <summary>
    /// Registers the type name as the preferred workflow JSON alias.
    /// </summary>
    public static WorkflowJsonTypeOptions AddTypeAlias<T>(this WorkflowJsonTypeOptions options) => options.RegisterTypeAlias(typeof(T), typeof(T).Name);

    /// <summary>
    /// Registers a preferred workflow JSON alias and the current simple assembly-qualified name as a legacy identifier.
    /// </summary>
    public static WorkflowJsonTypeOptions AddTypeAliasWithLegacyName(this WorkflowJsonTypeOptions options, Type type, string alias)
    {
        options.RegisterTypeAlias(type, alias);
        options.RegisterLegacySimpleAssemblyQualifiedName(type);
        return options;
    }

    /// <summary>
    /// Registers a preferred workflow JSON alias and the current simple assembly-qualified name as a legacy identifier.
    /// </summary>
    public static WorkflowJsonTypeOptions AddTypeAliasWithLegacyName<T>(this WorkflowJsonTypeOptions options, string alias) => options.AddTypeAliasWithLegacyName(typeof(T), alias);

    /// <summary>
    /// Registers the current simple assembly-qualified name as a compatibility identifier.
    /// </summary>
    public static WorkflowJsonTypeOptions AddLegacySimpleAssemblyQualifiedName<T>(this WorkflowJsonTypeOptions options) => options.RegisterLegacySimpleAssemblyQualifiedName(typeof(T));

    /// <summary>
    /// Registers the current simple assembly-qualified name as a compatibility identifier.
    /// </summary>
    public static WorkflowJsonTypeOptions AddLegacySimpleAssemblyQualifiedName(this WorkflowJsonTypeOptions options, Type type) => options.RegisterLegacySimpleAssemblyQualifiedName(type);

    /// <summary>
    /// Registers the current simple assembly-qualified name as the preferred alias for compatibility-only types.
    /// </summary>
    public static WorkflowJsonTypeOptions AddSimpleAssemblyQualifiedTypeAlias(this WorkflowJsonTypeOptions options, Type type)
    {
        return options.RegisterTypeAlias(type, type.GetSimpleAssemblyQualifiedName());
    }
}
