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
    }
}