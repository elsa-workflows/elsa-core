using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Contracts;

public interface IActivityDescriber
{
    ValueTask<ActivityDescriptor> DescribeActivityAsync([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type activityType, CancellationToken cancellationToken = default);
    OutputDescriptor DescribeOutputProperty(PropertyInfo propertyInfo);
    InputDescriptor DescribeInputProperty(PropertyInfo propertyInfo);
    IEnumerable<InputDescriptor> DescribeInputProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type activityType);
    IEnumerable<OutputDescriptor> DescribeOutputProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type activityType);
}