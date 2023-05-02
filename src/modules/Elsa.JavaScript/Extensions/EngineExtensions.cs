using Jint;
using Jint.Runtime.Interop;

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
}