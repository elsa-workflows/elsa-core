using Elsa.Management.Models;

namespace Elsa.Management.Contracts;

public interface ITriggerDescriber
{
    ValueTask<TriggerDescriptor> DescribeTriggerAsync(Type triggerType, CancellationToken cancellationToken = default);
}