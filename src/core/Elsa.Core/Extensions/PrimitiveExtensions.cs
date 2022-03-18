using System;
using System.Reflection;

namespace Elsa 
{
    public static class PrimitiveExtensions 
    {
        public static Type GetTypeOfNullable(this Type type)
        {
            return type.GetTypeInfo().GenericTypeArguments[0];
        }
    }
}