using System;
using System.Linq;
using System.Reflection;

namespace Elsa.Persistence.DocumentDb.Extensions
{
    internal static class TypeExtension
    {
        internal static T GetConstantValue<T>(this Type type, string fieldName) where T : class
        {
            var fieldInfo = type
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .FirstOrDefault(fi => fi.IsLiteral && !fi.IsInitOnly && fi.Name == fieldName);
            return fieldInfo != null && fieldInfo.GetRawConstantValue() is T constantValue ? constantValue : default;
        }
    }
}
