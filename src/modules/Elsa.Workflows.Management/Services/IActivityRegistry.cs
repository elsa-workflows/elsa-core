using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Management.Services;

public interface IActivityRegistry
{
    void Add(Type providerType, ActivityDescriptor descriptor);
    void AddMany(Type providerType, IEnumerable<ActivityDescriptor> descriptors);
    void Clear();
    void ClearProvider(Type providerType);
    IEnumerable<ActivityDescriptor> ListAll();
    IEnumerable<ActivityDescriptor> ListByProvider(Type providerType);
    ActivityDescriptor? Find(Func<ActivityDescriptor, bool> predicate);
    ActivityDescriptor? Find(string type, int version);
}