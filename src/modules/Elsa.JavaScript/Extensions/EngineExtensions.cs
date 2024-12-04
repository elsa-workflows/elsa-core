using Elsa.JavaScript.Helpers;
using Elsa.JavaScript.Options;
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
    public static void RegisterType<T>(this Engine engine) => engine.SetValue(typeof(T).Name, TypeReference.CreateTypeReference(engine, typeof(T)));
    
    /// <summary>
    /// Register the specified type <c>T</c> with the engine.
    /// </summary>
    public static void RegisterType(this Engine engine, Type type) => engine.SetValue(type.Name, TypeReference.CreateTypeReference(engine, type));

    internal static void SyncVariablesContainer(this Engine engine, IOptions<JintOptions> options, string name, object? value)
    {
        if (!options.Value.DisableWrappers)
        {
            // To ensure both variable accessor syntaxes work, we need to update the variables container in the engine as well as the context to keep them in sync.
            var variablesContainer = (IDictionary<string, object?>)engine.GetValue("variables").ToObject()!;
            variablesContainer[name] = ObjectConverterHelper.ProcessVariableValue(engine, value);
            engine.SetValue("variables", variablesContainer);
        }
    }
}