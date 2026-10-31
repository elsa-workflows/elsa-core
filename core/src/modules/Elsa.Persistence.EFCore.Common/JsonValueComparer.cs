using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Elsa.Persistence.EFCore;

/// <summary>
/// Compares two objects.
/// Required to make EF Core change tracking work for complex value converted objects.
/// </summary>
/// <remarks>
/// For objects that implement <see cref="ICloneable"/> and <see cref="IEquatable{T}"/>,
/// those implementations will be used for cloning and equality.
/// For plain objects, fall back to deep equality comparison using JSON serialization
/// (safe, but inefficient).
/// </remarks>
public class JsonValueComparer<T>() : ValueComparer<T>((t1, t2) => DoEquals(t1!, t2!),
    t => DoGetHashCode(t),
    t => DoGetSnapshot(t))
{

    private static string Json(T instance) {
        return JsonSerializer.Serialize(instance);
    }

    private static T DoGetSnapshot(T instance) {

        if (instance is ICloneable cloneable)
            return (T)cloneable.Clone();

        return JsonSerializer.Deserialize<T>(Json(instance))!;
    }

    private static int DoGetHashCode(T instance) {

        if (instance is IEquatable<T>)
            return instance.GetHashCode();

        return Json(instance).GetHashCode();

    }

    private static bool DoEquals(T left, T right) {

        if (left is IEquatable<T> equatable)
            return equatable.Equals(right);

        return Json(left).Equals(Json(right));
    }
}