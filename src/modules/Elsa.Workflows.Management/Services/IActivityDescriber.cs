using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Management.Services;

public interface IActivityDescriber
{
    ValueTask<ActivityDescriptor> DescribeActivityAsync([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type activityType, CancellationToken cancellationToken = default);
    OutputDescriptor DescribeOutputProperty(PropertyInfo propertyInfo);
    IEnumerable<InputDescriptor> DescribeInputProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type activityType);
    IEnumerable<OutputDescriptor> DescribeOutputProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type activityType);
}