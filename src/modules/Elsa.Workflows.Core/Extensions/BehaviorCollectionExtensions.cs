using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core;

public static class BehaviorCollectionExtensions
{
    public static void Add<T>(this ICollection<IBehavior> behaviors) where T : IBehavior => behaviors.Add((T)Activator.CreateInstance(typeof(T))!);

    public static void Remove<T>(this ICollection<IBehavior> behaviors) where T : IBehavior
    {
        var behaviorsToRemove = behaviors.Where(x => x is T).ToList();

        foreach (var behavior in behaviorsToRemove) behaviors.Remove(behavior);
    }
}