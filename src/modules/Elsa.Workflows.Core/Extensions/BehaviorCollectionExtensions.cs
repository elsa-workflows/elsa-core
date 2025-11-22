using Elsa.Workflows;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class BehaviorCollectionExtensions
{
    extension(ICollection<IBehavior> behaviors)
    {
        public void Add<T>(IActivity owner) where T : IBehavior => behaviors.Add((T)Activator.CreateInstance(typeof(T), owner)!);

        public void Remove<T>() where T : IBehavior
        {
            var behaviorsToRemove = behaviors.Where(x => x is T).ToList();

            foreach (var behavior in behaviorsToRemove) behaviors.Remove(behavior);
        }
    }
}