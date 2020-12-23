using System;
using System.Reflection;

using Elsa.Activities.Entity.Attributes;

namespace Elsa.Activities.Entity.Extensions
{
    public static class TypeExtensions
    {
        public static string GetEntityName(this Type type, bool inherit = false)
        {
            var attribute = type.GetCustomAttribute<EntityNameAttribute>(inherit);
            return attribute?.Name ?? type.FullName!;
        }
    }
}
