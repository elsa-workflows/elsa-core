using Elsa.Expressions.JavaScript.Helpers;
using Elsa.Expressions.JavaScript.Options;
using Jint;
using Jint.Runtime.Interop;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extends <see cref="Engine"/>.
/// </summary>
public static class EngineExtensions
{
    /// <summary>
    /// Register the specified type <c>T</c> with the engine.
    /// </summary>
    public static void RegisterType<T>(this Engine engine) => engine.RegisterType(typeof(T));

    /// <summary>
    /// Register the specified type with the engine, under its type name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Types whose name cannot be written as a JavaScript identifier are skipped. A constructed generic type such
    /// as <c>IDictionary&lt;string, object&gt;</c> is named <c>IDictionary`2</c> and an array type such as
    /// <c>byte[]</c> is named <c>Byte[]</c>. Registered, those would only be reachable through bracket notation —
    /// <c>globalThis['Byte[]']</c> — and every constructed generic type of the same arity would claim the same
    /// global, so the last one registered would silently win.
    /// </para>
    /// <para>
    /// A name that is already taken is left alone. Registering the same type twice is therefore a no-op, and a
    /// global installed before this method is called is never replaced. Built-in type globals are registered
    /// lazily through <see cref="EngineOptionsExtensions.RegisterType(Jint.Options, Type)"/> while the engine is
    /// being constructed, so host callbacks that run afterwards can replace them deliberately.
    /// </para>
    /// </remarks>
    public static void RegisterType(this Engine engine, Type type)
    {
        var name = type.Name;

        if (!IsUsableAsIdentifier(name))
            return;

        // Leave any value that the host or JavaScript already installed under this name untouched.
        if (engine.Global.HasOwnProperty(name))
            return;

        engine.SetValue(name, TypeReference.CreateTypeReference(engine, type));
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

    internal static void SyncVariablesContainer(this Engine engine, IOptions<JintOptions> options, string name, object? value)
    {
        if (options.Value.DisableWrappers || options.Value.DisableVariableCopying)
            return;

        // To ensure both variable accessor syntaxes work, we need to update the variables container in the engine as well as the context to keep them in sync.
        var variablesContainer = (IDictionary<string, object?>)engine.GetValue("variables").ToObject()!;
        variablesContainer[name] = ObjectConverterHelper.ProcessVariableValue(engine, value);
        engine.SetValue("variables", variablesContainer);
    }
}
