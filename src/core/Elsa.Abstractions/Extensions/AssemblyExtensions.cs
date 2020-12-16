using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace System.Reflection
{
    public static class AssemblyExtensions
    {
        public static IEnumerable<Type> GetAllWithInterface(this Assembly assembly, Type @interface) => assembly.GetTypes().Where(t => t.IsClass && t.IsAbstract == false && t.GetInterfaces().Contains(@interface));
        public static IEnumerable<Type> GetAllWithInterface<TType>(this Assembly assembly) => assembly.GetAllWithInterface(typeof(TType));
    }
}
