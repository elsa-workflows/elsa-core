using Elsa.Management.Models;

namespace Elsa.Management.Contracts;

public interface IActivityDescriber
{
    ValueTask<ActivityDescriptor> DescribeActivityAsync(Type activityType, CancellationToken cancellationToken = default);
}