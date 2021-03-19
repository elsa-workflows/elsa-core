using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// The System.Reflection namespace is not used here because of the likelihood that another library will use identical functions. This can cause conflicts.
namespace Elsa
{
    public static class AssemblyExtensions
    {
        public static IEnumerable<Type> GetAllWithInterface(this Assembly assembly, Type @interface) => assembly.GetTypes().Where(t => t.IsClass && t.IsAbstract == false && t.GetInterfaces().Contains(@interface));
        public static IEnumerable<Type> GetAllWithBaseClass(this Assembly assembly, Type baseClass) => assembly.GetTypes().Where(baseClass.IsAssignableFrom);
        public static IEnumerable<Type> GetAllWithInterface<TType>(this Assembly assembly) => assembly.GetAllWithInterface(typeof(TType));
        public static IEnumerable<Type> GetAllWithBaseClass<TType>(this Assembly assembly) => assembly.GetAllWithBaseClass(typeof(TType));
    }
}
