using Elsa.Workflows.Core.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions for <see cref="ActivityDescriptor"/>.
/// </summary>
public static class ActivityDescriptorExtensions
{
    /// <summary>
    /// Returns the specified attribute from the activity descriptor.
    /// </summary>
    public static T GetAttribute<T>(this ActivityDescriptor activityDescriptor) where T:Attribute
    {
        return (T)activityDescriptor.Attributes.First(x => x.GetType() == typeof(T));
    }
}