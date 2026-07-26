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
    /// Types whose name is not usable as a JavaScript identifier are skipped. A constructed generic type such as
    /// <c>IDictionary&lt;string, object&gt;</c> is named <c>IDictionary`2</c> and an array type such as
    /// <c>byte[]</c> is named <c>Byte[]</c>; neither can be referenced from a script, and every constructed
    /// generic type of the same arity would claim the same global. Registering the same type twice is a no-op.
    /// </remarks>
    public static void RegisterType(this Engine engine, Type type)
    {
        var name = type.Name;

        if (!IsUsableAsIdentifier(name))
            return;

        // The type registrations are contributed by several independent handlers, which overlap: without this
        // check, the overlapping types would each be re-created and re-assigned for every expression evaluation.
        if (engine.GetValue(name) is TypeReference registered && registered.ReferenceType == type)
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
