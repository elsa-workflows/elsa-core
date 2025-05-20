// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ListConverter
{
    public static T ConvertTo<T>(this IEnumerable<object?> items) where T : IEnumerable<object?> => (T)ConvertTo(items, typeof(T));

    public static object ConvertTo(this IEnumerable<object?> items, Type type)
    {
        var containedType = type.GenericTypeArguments.First();
        var enumerableType = typeof(Enumerable);
        var castMethod = enumerableType.GetMethod(nameof(Enumerable.Cast))!.MakeGenericMethod(containedType);
        var toListMethod = enumerableType.GetMethod(nameof(Enumerable.ToList))!.MakeGenericMethod(containedType);
        var castedItems = castMethod.Invoke(null, new object?[] { items });

        return toListMethod.Invoke(null!, new[] { castedItems })!;
    }
}