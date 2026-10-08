using Jint;
using Jint.Runtime.Interop;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extends <see cref="Jint.Options"/>.
/// </summary>
public static class EngineOptionsExtensions
{
    /// <summary>
    /// Registers the specified type <c>T</c> with every engine built from these options.
    /// </summary>
    public static Jint.Options RegisterType<T>(this Jint.Options options) => options.RegisterType(typeof(T));

    /// <summary>
    /// Registers the specified type with every engine built from these options, under its type name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The type reference is built the first time a script reads the name. Registering a type therefore costs a
    /// property definition rather than the reflection needed to describe the type, which matters because a fresh
    /// engine is built for every expression evaluation and most expressions use none of the registered types.
    /// </para>
    /// <para>
    /// Types whose name cannot be written as a JavaScript identifier are skipped: a constructed generic type such
    /// as <c>IDictionary&lt;string, object&gt;</c> is named <c>IDictionary`2</c> and an array type such as
    /// <c>byte[]</c> is named <c>Byte[]</c>. Registered, those would only be reachable through bracket notation —
    /// <c>globalThis['Byte[]']</c> — and every constructed generic type of the same arity would claim the same
    /// global.
    /// </para>
    /// <para>
    /// Registering the same name twice is allowed and the last registration wins. Several handlers contribute type
    /// registrations and their sets overlap — <c>Guid</c>, <c>DateTime</c>, <c>DateTimeOffset</c>, <c>TimeSpan</c>
    /// and <c>LogPersistenceMode</c> are registered both as common types and as workflow variable types — but a
    /// duplicate now costs one property definition and describes nothing, and both registrations name the same
    /// <see cref="Type"/>, so which one survives is not observable. Registration happens while the engine is being
    /// built, so every host extension point — the per-evaluation <c>configureEngine</c> callback,
    /// <c>JintOptions.ConfigureEngine</c> and <c>JintOptions.ConfigureEngineOptions</c> — runs afterwards and a
    /// global installed there is never replaced by a type registration.
    /// </para>
    /// </remarks>
    public static Jint.Options RegisterType(this Jint.Options options, Type type)
    {
        var name = type.Name;

        if (!IsUsableAsIdentifier(name))
            return options;

        return options.AddLazyGlobal(name, engine => TypeReference.CreateTypeReference(engine, type));
    }

    private static bool IsUsableAsIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name) || (!char.IsLetter(name[0]) && name[0] != '_' && name[0] != '$'))
            return false;

        foreach (var c in name)
        {
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '$')
                return false;
        }

        return true;
    }
}
