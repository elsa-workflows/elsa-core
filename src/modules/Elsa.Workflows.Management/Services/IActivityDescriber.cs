using System.Diagnostics.CodeAnalysis;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Management.Services;

public interface IActivityDescriber
{
    ValueTask<ActivityDescriptor> DescribeActivityAsync([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type activityType, CancellationToken cancellationToken = default);
}