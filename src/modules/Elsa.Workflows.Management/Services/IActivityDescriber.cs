using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Management.Services;

public interface IActivityDescriber
{
    ValueTask<ActivityDescriptor> DescribeActivityAsync(Type activityType, CancellationToken cancellationToken = default);
}