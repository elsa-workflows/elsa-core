using Jint;
using Jint.Runtime.Interop;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class EngineExtensions
{
    public static void RegisterType<T>(this Engine engine) => engine.SetValue(typeof(T).Name, TypeReference.CreateTypeReference(engine, typeof(T)));
}