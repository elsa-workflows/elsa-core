using Jint;
using Jint.Runtime.Interop;

namespace Elsa.Scripting.JavaScript.Extensions
{
    public static class EngineExtensions
    {
        public static void RegisterType<T>(this Engine engine) => engine.SetValue(typeof(T).Name, TypeReference.CreateTypeReference(engine, typeof(T)));
    }
}