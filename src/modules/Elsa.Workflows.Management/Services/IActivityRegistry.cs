using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Management.Services;

public interface IActivityRegistry
{
    void Add(object provider, ActivityDescriptor descriptor);
    void AddMany(object provider, IEnumerable<ActivityDescriptor> descriptors);
    void Clear();
    void ClearProvider(object provider);
    IEnumerable<ActivityDescriptor> ListAll();
    IEnumerable<ActivityDescriptor> ListByProvider(object provider);
    ActivityDescriptor? Find(Func<ActivityDescriptor, bool> predicate);
    ActivityDescriptor? Find(string activityType);
}