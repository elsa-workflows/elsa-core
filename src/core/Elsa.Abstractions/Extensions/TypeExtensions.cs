using System;
using System.Reflection;
using Elsa.Attributes;

namespace Elsa
{
    public static class TypeExtensions
    {
        public static string GetContextTypeName(this Type type, bool inherit = false)
        {
            var attribute = type.GetCustomAttribute<ContextTypeNameAttribute>(inherit);
            return attribute?.Name ?? type.FullName!;
        }
    }
}
