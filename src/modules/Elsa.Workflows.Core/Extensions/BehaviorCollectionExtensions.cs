using Elsa.Workflows.Core.Contracts;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class BehaviorCollectionExtensions
{
    public static void Add<T>(this ICollection<IBehavior> behaviors, IActivity owner) where T : IBehavior => behaviors.Add((T)Activator.CreateInstance(typeof(T), owner)!);

    public static void Remove<T>(this ICollection<IBehavior> behaviors) where T : IBehavior
    {
        var behaviorsToRemove = behaviors.Where(x => x is T).ToList();

        foreach (var behavior in behaviorsToRemove) behaviors.Remove(behavior);
    }
}