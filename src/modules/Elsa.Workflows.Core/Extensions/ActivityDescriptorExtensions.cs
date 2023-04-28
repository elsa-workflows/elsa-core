using Elsa.Workflows.Core.Contracts;
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
    public static T? GetAttribute<T>(this ActivityDescriptor activityDescriptor) where T : Attribute
    {
        return (T?)activityDescriptor.Attributes.FirstOrDefault(x => x.GetType() == typeof(T));
    }

    /// <summary>
    /// Returns each input from the specified activity.
    /// </summary>
    public static IEnumerable<InputDescriptor> GetWrappedInputPropertyDescriptors(this ActivityDescriptor activityDescriptor, IActivity activity)
    {
        return activityDescriptor.Inputs.Where(x => x.IsWrapped);
    }
    
    /// <summary>
    /// Returns each input from the specified activity.
    /// </summary>
    public static IDictionary<string, Input?> GetWrappedInputProperties(this ActivityDescriptor activityDescriptor, IActivity activity)
    {
        var wrappedInputDescriptors = activityDescriptor.Inputs.Where(x => x.IsWrapped).ToList();
        var inputLookup = wrappedInputDescriptors.ToDictionary(x => x.Name, x => (Input?)x.ValueGetter(activity));
        return inputLookup;
    }
    
    /// <summary>
    /// Returns each input descriptor from the specified activity.
    /// </summary>
    public static InputDescriptor? GetWrappedInputPropertyDescriptor(this ActivityDescriptor activityDescriptor, IActivity activity, string name)
    {
        return activityDescriptor.Inputs.FirstOrDefault(x => x.Name == name && x.IsWrapped);
    }
    
    /// <summary>
    /// Returns each input from the specified activity.
    /// </summary>
    public static Input? GetWrappedInputProperty(this ActivityDescriptor activityDescriptor, IActivity activity, string name)
    {
        var inputDescriptor = activityDescriptor.Inputs.FirstOrDefault(x => x.Name == name && x.IsWrapped);
        return inputDescriptor?.ValueGetter(activity) as Input;
    }
}