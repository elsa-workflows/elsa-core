using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Creates instances of <see cref="ActivityDescriptor" /> for a given activity type.
/// </summary>
public interface IActivityDescriber
{
    /// <summary>
    /// Describes the specified activity type.
    /// </summary>
    /// <param name="activityType">The activity type to describe.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The activity descriptor.</returns>
    ValueTask<ActivityDescriptor> DescribeActivityAsync([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type activityType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates an instance of <see cref="OutputDescriptor"/> for the specified property. 
    /// </summary>
    /// <param name="propertyInfo">The property to describe.</param>
    /// <returns>The output descriptor.</returns>
    OutputDescriptor DescribeOutputProperty(PropertyInfo propertyInfo);
    
    /// <summary>
    /// Creates an instance of <see cref="InputDescriptor"/> for the specified property.
    /// </summary>
    /// <param name="propertyInfo">The property to describe.</param>
    /// <returns>The input descriptor.</returns>
    InputDescriptor DescribeInputProperty(PropertyInfo propertyInfo);
    
    /// <summary>
    /// Describes the input properties of the specified activity type.
    /// </summary>
    /// <param name="activityType">The activity type to describe.</param>
    /// <returns>The input descriptors.</returns>
    IEnumerable<InputDescriptor> DescribeInputProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type activityType);
    
    /// <summary>
    /// Describes the output properties of the specified activity type.
    /// </summary>
    /// <param name="activityType">The activity type to describe.</param>
    /// <returns>The output descriptors.</returns>
    IEnumerable<OutputDescriptor> DescribeOutputProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type activityType);

    /// <summary>
    /// Gets the input properties of the specified activity type.
    /// </summary>
    /// <param name="activityType">The activity type.</param>
    /// <returns>The input properties.</returns>
    IEnumerable<PropertyInfo> GetInputProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type activityType);

    /// <summary>
    /// Gets the output properties of the specified activity type.
    /// </summary>
    /// <param name="activityType">The activity type.</param>
    /// <returns>The output properties.</returns>
    IEnumerable<PropertyInfo> GetOutputProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type activityType);
}