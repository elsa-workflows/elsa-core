using System;
using System.Collections.Generic;

namespace Elsa.Scripting.JavaScript.Extensions
{
    public static class TypeExtensions
    {
        private static readonly HashSet<Type> NumericTypes = new()
        {
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(double),
            typeof(decimal),
            typeof(float)
        };

        public static bool IsNumeric(this Type type)
        {
            return NumericTypes.Contains(type);
        }
        
        public static bool IsObject(this Type type)
        {
            return type == typeof(object);
        }


        /// <summary>
        /// Determines whether the specified type is a nullable value type (e.g., Nullable&lt;T&gt;).
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is a nullable value type; otherwise, false.</returns>
        public static bool IsNullableType(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
    }
}