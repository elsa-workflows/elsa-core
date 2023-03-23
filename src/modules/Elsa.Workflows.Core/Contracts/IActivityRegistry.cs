using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Stores all activity descriptors available to the system.
/// </summary>
public interface IActivityRegistry : IActivityProvider
{
    void Add(Type providerType, ActivityDescriptor descriptor);
    void AddMany(Type providerType, IEnumerable<ActivityDescriptor> descriptors);
    void Clear();
    void ClearProvider(Type providerType);
    IEnumerable<ActivityDescriptor> ListAll();
    IEnumerable<ActivityDescriptor> ListByProvider(Type providerType);
    ActivityDescriptor? Find(string type);
    ActivityDescriptor? Find(string type, int version);
    ActivityDescriptor? Find(Func<ActivityDescriptor, bool> predicate);
    IEnumerable<ActivityDescriptor> FindMany(Func<ActivityDescriptor, bool> predicate);
    void Register(ActivityDescriptor descriptor);
    Task RegisterAsync(Type activityType, CancellationToken cancellationToken = default);
    Task RegisterAsync(IEnumerable<Type> activityTypes, CancellationToken cancellationToken = default);
}