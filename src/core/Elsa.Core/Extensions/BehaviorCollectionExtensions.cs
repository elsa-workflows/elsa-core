using Elsa.Contracts;

namespace Elsa;

public static class BehaviorCollectionExtensions
{
    public static void Add<T>(this ICollection<IBehavior> behaviors) where T : IBehavior => behaviors.Add((T)Activator.CreateInstance(typeof(T))!);
}