using Elsa.Management.Models;

namespace Elsa.Management.Services;

public interface IActivityDescriber
{
    ValueTask<ActivityDescriptor> DescribeActivityAsync(Type activityType, CancellationToken cancellationToken = default);
}