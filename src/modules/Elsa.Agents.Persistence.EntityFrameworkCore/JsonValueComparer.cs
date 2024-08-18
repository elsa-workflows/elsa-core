using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Elsa.Agents.Persistence.EntityFrameworkCore;

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
internal class JsonValueComparer<T> : ValueComparer<T> {

    private static string Json(T instance) {
        return JsonSerializer.Serialize(instance);
    }

    private static T DoGetSnapshot(T instance) {

        if (instance is ICloneable cloneable)
            return (T)cloneable.Clone();

        var result = (T)JsonSerializer.Deserialize<T>(Json(instance));
        return result;

    }

    private static int DoGetHashCode(T instance) {

        if (instance is IEquatable<T>)
            return instance.GetHashCode();

        return Json(instance).GetHashCode();

    }

    private static bool DoEquals(T left, T right) {

        if (left is IEquatable<T> equatable)
            return equatable.Equals(right);

        var result = Json(left).Equals(Json(right));
        return result;

    }

    public JsonValueComparer() : base(
        (t1, t2) => DoEquals(t1, t2), 
        t => DoGetHashCode(t), 
        t => DoGetSnapshot(t)) {
    }

}