using Elsa.Dsl.Contracts;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.Dsl.Extensions;

public static class TypeSystemExtensions
{
    public static TypeDescriptor Register<T>(this ITypeSystem typeSystem, string? typeName = default) => typeSystem.Register(typeName ?? typeof(T).Name, typeof(T));

    public static TypeDescriptor Register(this ITypeSystem typeSystem, string typeName, Type type)
    {
        var kind = GetKind(type);
        var descriptor = new TypeDescriptor(typeName, type, kind);
        typeSystem.Register(descriptor);
        return descriptor;
    }

    private static TypeKind GetKind(Type type)
    {
        var kind = TypeKind.Unknown;
        var isActivity = typeof(IActivity).IsAssignableFrom(type);
        var isTrigger = typeof(IEventGenerator).IsAssignableFrom(type);
        var isPrimitive = type.IsPrimitive;
        var isObject = type.IsClass || type.IsValueType; 

        if (isActivity || isTrigger)
        {
            if (isActivity)
                kind |= TypeKind.Activity;

            if (isTrigger)
                kind |= TypeKind.Trigger;
        }
        else if (isPrimitive)
            kind = TypeKind.Primitive;
        else if(isObject)
            kind = TypeKind.Object;

        return kind;
    }
}