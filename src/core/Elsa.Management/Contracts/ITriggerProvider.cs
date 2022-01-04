using Elsa.Management.Models;

namespace Elsa.Management.Contracts;

public interface ITriggerProvider
{
    ValueTask<IEnumerable<TriggerDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default);
}